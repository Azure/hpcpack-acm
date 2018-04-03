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
    using System.Diagnostics;
    using Microsoft.Extensions.Options;

    internal class NodeAgentWorker : TaskItemWorker, IWorker
    {
        private CloudTable jobsTable;
        private CloudTable nodesTable;

        private NodeCommunicator communicator;
        public TaskMonitor Monitor { get; set; }
        private readonly NodeAgentWorkerOptions options;

        public NodeAgentWorker(IOptions<NodeAgentWorkerOptions> options, TaskMonitor monitor) : base(options.Value)
        {
            this.options = options.Value;
            this.Monitor = monitor;
        }

        public override async Task InitializeAsync(CancellationToken token)
        {
            this.communicator = new NodeCommunicator(this.Logger, this.Configuration, this.options);
            this.jobsTable = await this.Utilities.GetOrCreateJobsTableAsync(token);
            this.nodesTable = await this.Utilities.GetOrCreateNodesTableAsync(token);
            this.Source = new QueueTaskItemSource(
                await this.Utilities.GetOrCreateNodeDispatchQueueAsync(this.ServerOptions.HostName, token),
                TimeSpan.FromSeconds(this.options.VisibleTimeoutSeconds),
                TimeSpan.FromSeconds(this.options.RetryIntervalSeconds));

            await base.InitializeAsync(token);
        }

        public override async Task ProcessTaskItemAsync(TaskItem taskItem, CancellationToken token)
        {
            var task = taskItem.GetMessage<InternalTask>();
            var nodeName = this.ServerOptions.HostName;
            Debug.Assert(nodeName == task.Node, "NodeName mismatch");
            var taskKey = this.Utilities.GetTaskKey(task.JobId, task.Id, task.RequeueCount);
            using (this.Logger.BeginScope("Do work for InternalTask {0} on node {1}", taskKey, nodeName))
            {
                // TODO: make sure invisible.
                var cmd = task.CommandLine;
                Logger.LogInformation("Executing command {0}", cmd);

                var resultKey = this.Utilities.GetJobResultKey(nodeName, taskKey);
                var taskResultBlob = await this.Utilities.CreateOrReplaceTaskOutputBlobAsync(task.JobType.ToString().ToLowerInvariant(), task.JobId, resultKey, token);
                using (var monitor = this.Monitor.StartMonitorTask(taskKey, async (output, cancellationToken) =>
                {
                    try
                    {
                        await taskResultBlob.AppendTextAsync(output, Encoding.UTF8, null, null, null, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        this.Logger.LogError(ex, "Error happened when append to blob {0}", taskResultBlob.Name);
                    }
                }))
                {
                    this.Logger.LogInformation("Call startjobandtask for task {0}", taskKey);
                    var jobPartitionName = this.Utilities.GetJobPartitionKey($"{task.JobType}", task.JobId);
                    var nodePartitionName = this.Utilities.GetNodePartitionKey(nodeName);

                    var taskResultArgs = new ComputeNodeTaskCompletionEventArgs(nodeName, task.JobId, null) { State = TaskState.Dispatching };
                    var taskResultEntity = new JsonTableEntity(jobPartitionName, resultKey, taskResultArgs);
                    var result = await jobsTable.ExecuteAsync(TableOperation.InsertOrReplace(taskResultEntity), null, null, token);
                    this.Logger.LogInformation("Saved task result {0} to jobs table, status code {1}", resultKey, result.HttpStatusCode);
                    if (!result.IsSuccessfulStatusCode()) { return; }

                    var nodeResultEntity = new JsonTableEntity(nodePartitionName, resultKey, taskResultArgs);
                    result = await nodesTable.ExecuteAsync(TableOperation.InsertOrReplace(nodeResultEntity), null, null, token);
                    this.Logger.LogInformation("Saved task result {0} to nodes table, status code {1}", resultKey, result.HttpStatusCode);
                    if (!result.IsSuccessfulStatusCode()) { return; }

                    await this.communicator.StartJobAndTaskAsync(
                         nodeName,
                         new StartJobAndTaskArg(new int[0], task.JobId, task.Id), task.UserName, task.Password,
                         new Common.ProcessStartInfo(cmd, "", "", $"{this.communicator.Options.AgentUriBase}/output/{taskKey}",
                            "", new System.Collections.Hashtable(), new long[0], task.RequeueCount), token);

                    taskResultArgs = new ComputeNodeTaskCompletionEventArgs(nodeName, task.JobId, null) { State = TaskState.Running };
                    taskResultEntity = new JsonTableEntity(jobPartitionName, resultKey, taskResultArgs);
                    result = await jobsTable.ExecuteAsync(TableOperation.InsertOrReplace(taskResultEntity), null, null, token);
                    this.Logger.LogInformation("Saved task result {0} to jobs table, status code {1}", resultKey, result.HttpStatusCode);
                    if (!result.IsSuccessfulStatusCode()) { return; }

                    nodeResultEntity = new JsonTableEntity(nodePartitionName, resultKey, taskResultArgs);
                    result = await nodesTable.ExecuteAsync(TableOperation.InsertOrReplace(nodeResultEntity), null, null, token);
                    this.Logger.LogInformation("Saved task result {0} to nodes table, status code {1}", resultKey, result.HttpStatusCode);
                    if (!result.IsSuccessfulStatusCode()) { return; }

                    this.Logger.LogInformation("Wait for response for job {0}, task {1}", task.JobId, taskKey);
                    taskResultArgs = await monitor.Execution;

                    this.Logger.LogInformation("Saving result for job {0}, task {1}", task.JobId, taskKey);

                    taskResultArgs.State = TaskState.Finished;
                    taskResultEntity = new JsonTableEntity(jobPartitionName, resultKey, taskResultArgs);
                    result = await jobsTable.ExecuteAsync(TableOperation.InsertOrReplace(taskResultEntity), null, null, token);
                    this.Logger.LogInformation("Saved task result {0} to jobs table, status code {1}", resultKey, result.HttpStatusCode);
                    if (!result.IsSuccessfulStatusCode()) { return; }

                    nodeResultEntity = new JsonTableEntity(nodePartitionName, resultKey, taskResultArgs);
                    result = await nodesTable.ExecuteAsync(TableOperation.InsertOrReplace(nodeResultEntity), null, null, token);
                    this.Logger.LogInformation("Saved task result {0} to nodes table, status code {1}", resultKey, result.HttpStatusCode);
                    if (!result.IsSuccessfulStatusCode()) { return; }

                    var queue = await this.Utilities.GetOrCreateTaskCompletionQueueAsync(token);
                    await queue.AddMessageAsync(new CloudQueueMessage(JsonConvert.SerializeObject(new TaskCompletionMessage()
                    {
                        JobId = task.JobId,
                        Id = task.Id,
                        ExitCode = taskResultArgs.TaskInfo.ExitCode,
                        JobType = task.JobType,
                        RequeueCount = task.RequeueCount,
                    }, Formatting.Indented)), null, null, null, null, token);
                }

                await taskItem.FinishAsync(token);
            }
        }
    }
}
