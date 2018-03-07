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
        private ILogger logger;
        private IConfiguration config;
        private CloudUtilities utilities;
        private CloudTable jobsTable;
        private CloudTable nodesTable;

        private IDictionary<string, CloudQueue> nodesJobQueue = new Dictionary<string, CloudQueue>();
        private NodeCommunicator communicator;

        public NodeAgentWorker(IConfiguration config, ILoggerFactory loggerFactory, CloudTable jobsTable, CloudTable nodesTable, CloudUtilities utilities)
        {
            this.config = config;
            this.logger = loggerFactory.CreateLogger<NodeAgentWorker>();
            this.communicator = new NodeCommunicator(loggerFactory, config);
            this.utilities = utilities;
            this.jobsTable = jobsTable;
            this.nodesTable = nodesTable;
        }

        public TaskMonitor Monitor { get; set; }

        public override async Task DoWorkAsync(TaskItem taskItem, CancellationToken token)
        {
            var job = taskItem.GetMessage<InternalJob>();
            var nodeName = Environment.MachineName.ToLowerInvariant();
            using (this.logger.BeginScope("Do work for InternalJob {0} on node {1}", job.Id, nodeName))
            {
                var tasks = Enumerable.Range(0, job.CommandLines.Length).Select(async taskId =>
                {
                    var cmd = job.CommandLines[taskId];
                    var taskKey = this.utilities.GetTaskKey(job.Id, taskId, job.RequeueCount);
                    using (var monitor = this.Monitor.StartMonitorTaskResult(taskKey))
                    {
                        await this.communicator.StartJobAndTaskAsync(
                            nodeName,
                            new StartJobAndTaskArg(null, job.Id, taskId),
                            "", "", new ProcessStartInfo(cmd, null, null, null, null, null, null, job.RequeueCount), token);

                        var taskResult = await monitor.Execution;

                        var resultKey = this.utilities.GetJobResultKey(nodeName, taskKey);
                        var jobTableEntity = new GenericTableEntity<ComputeNodeTaskCompletionEventArg>(
                            this.utilities.GetJobPartitionName(job.Id, $"{job.Type}"),
                            resultKey,
                            taskResult);

                        var result = await jobsTable.ExecuteAsync(TableOperation.InsertOrReplace(jobTableEntity), null, null, token);

                        // TODO: deal with return code.

                        this.logger.LogInformation("Saved task result {0} to jobs table, status code {1}", resultKey, result.HttpStatusCode);

                        var nodeTableEntity = new GenericTableEntity<ComputeNodeTaskCompletionEventArg>(
                            this.utilities.GetNodePartitionName(nodeName),
                            resultKey,
                            taskResult);

                        result = await nodesTable.ExecuteAsync(TableOperation.InsertOrReplace(nodeTableEntity), null, null, token);
                        this.logger.LogInformation("Saved task result {0} to nodes table, status code {1}", resultKey, result.HttpStatusCode);
                        // TODO: upload to storage
                    }
                });

                await Task.WhenAll(tasks);
            }
        }
    }
}
