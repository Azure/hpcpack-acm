namespace Microsoft.HpcAcm.Services.NodeAgent
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Microsoft.HpcAcm.Services.Common;
    using Microsoft.HpcAcm.Common.Dto;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Microsoft.HpcAcm.Common.Utilities;
    using Microsoft.WindowsAzure.Storage.Table;
    using Microsoft.WindowsAzure.Storage.Queue;
    using Newtonsoft.Json;
    using System.Linq;
    using Microsoft.Extensions.Configuration;

    internal class NodeAgentWorker : WorkerBase
    {
        private readonly ILogger logger;
        private readonly CloudUtilities utilities;
        private readonly CloudTable jobsTable;
        private readonly CloudTable nodesTable;

        private readonly NodeCommunicator communicator;

        public NodeAgentWorker(IConfiguration config, ILoggerFactory loggerFactory, CloudTable jobsTable, CloudTable nodesTable, CloudUtilities utilities)
        {
            this.Configuration = config;
            this.logger = loggerFactory.CreateLogger<NodeAgentWorker>();
            this.communicator = new NodeCommunicator(loggerFactory, config);
            this.utilities = utilities;
            this.jobsTable = jobsTable;
            this.nodesTable = nodesTable;
        }
        private IConfiguration Configuration { get; set; }

        public TaskMonitor Monitor { get; set; }

        public override async Task DoWorkAsync(TaskItem taskItem, CancellationToken token)
        {
            var job = taskItem.GetMessage<InternalJob>();
            var nodeName = Environment.MachineName.ToLowerInvariant();
            nodeName = "evanclinuxdev";
            using (this.logger.BeginScope("Do work for InternalJob {0} on node {1}", job.Id, nodeName))
            {
                logger.LogInformation("Executing job {0}", job.Id);
                var tasks = Enumerable.Range(0, job.CommandLines.Length).Select(async taskId =>
                {
                    var cmd = job.CommandLines[taskId];
                    logger.LogInformation("Executing command {0}, job {1}", cmd, job.Id);
                    var taskKey = this.utilities.GetTaskKey(job.Id, taskId, job.RequeueCount);
                    var taskResultBlob = await this.utilities.CreateOrReplaceTaskOutputBlobAsync(job.Id, taskKey, token);
                    using (var monitor = this.Monitor.StartMonitorTask(taskKey, async (output, cancellationToken) =>
                    {
                        try
                        {
                            await taskResultBlob.AppendTextAsync(output, null, null, null, null, cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            this.logger.LogError(ex, "Error happened when append to blob {0}", taskResultBlob.Name);
                        }
                    }))
                    {
                        this.logger.LogInformation("Call startjobandtask for job {0}, task {1}", job.Id, taskKey);
                        await this.communicator.StartJobAndTaskAsync(
                            nodeName,
                            new StartJobAndTaskArg(new int[0], job.Id, taskId),
                                "", "", new ProcessStartInfo(cmd, "", "", $"{this.communicator.Options.AgentUriBase}/message/{taskKey}",
                                "", new System.Collections.Hashtable(), new long[0], job.RequeueCount), token);

                        this.logger.LogInformation("Wait for response for job {0}, task {1}", job.Id, taskKey);
                        var taskResult = await monitor.Execution;

                        this.logger.LogInformation("Saving result for job {0}, task {1}", job.Id, taskKey);
                        var resultKey = this.utilities.GetJobResultKey(nodeName, taskKey);
                        var jobPartitionName = this.utilities.GetJobPartitionName(job.Id, $"{job.Type}");

                        var jobEntity = new JsonTableEntity(jobPartitionName, resultKey, taskResult);

                        var result = await jobsTable.ExecuteAsync(TableOperation.InsertOrReplace(jobEntity), null, null, token);

                        // TODO: deal with return code.

                        this.logger.LogInformation("Saved task result {0} to jobs table, status code {1}", resultKey, result.HttpStatusCode);

                        var nodePartitionName = this.utilities.GetNodePartitionName(nodeName);
                        var nodeEntity = new JsonTableEntity(nodePartitionName, resultKey, taskResult);

                        result = await nodesTable.ExecuteAsync(TableOperation.InsertOrReplace(nodeEntity), null, null, token);
                        this.logger.LogInformation("Saved task result {0} to nodes table, status code {1}", resultKey, result.HttpStatusCode);
                        // TODO: upload to storage
                    }
                });

                await Task.WhenAll(tasks);
            }
        }
    }
}
