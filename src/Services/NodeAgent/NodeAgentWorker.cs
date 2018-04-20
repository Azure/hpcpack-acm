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
        private string nodesPartitionName;
        private string jobsPartitionName;

        private NodeCommunicator communicator;
        public TaskMonitor Monitor { get; set; }
        private readonly NodeAgentWorkerOptions options;
        private const int MaxRawResultLength = 4096;

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

        private async Task<bool> PersistTaskResult(string resultKey, object result, CancellationToken token)
        {
            if (!await jobsTable.InsertOrReplaceAsJsonAsync(this.jobsPartitionName, resultKey, result, token)) { return false; }
            this.Logger.LogInformation("Saved task result {0} to jobs table", resultKey);
            if (!await nodesTable.InsertOrReplaceAsJsonAsync(this.nodesPartitionName, resultKey, result, token)) { return false; }
            this.Logger.LogInformation("Saved task result {0} to nodes table", resultKey);
            return true;
        }

        public override async Task<bool> ProcessTaskItemAsync(TaskItem taskItem, CancellationToken token)
        {
            // TODO: make task processor transient and support cancel.
            var task = taskItem.GetMessage<InternalTask>();
            var nodeName = this.ServerOptions.HostName;
            Debug.Assert(nodeName == task.Node, "NodeName mismatch");
            var taskKey = this.Utilities.GetTaskKey(task.JobId, task.Id, task.RequeueCount);
            var taskResultKey = this.Utilities.GetTaskResultKey(task.JobId, task.Id, task.RequeueCount);
            using (this.Logger.BeginScope("Do work for InternalTask {0} on node {1}", taskKey, nodeName))
            {
                var cmd = task.CommandLine;
                Logger.LogInformation("Executing command {0}", cmd);

                var nodeTaskResultKey = this.Utilities.GetNodeTaskResultKey(nodeName, task.JobId, task.RequeueCount, task.Id);
                var taskResultBlob = await this.Utilities.CreateOrReplaceTaskOutputBlobAsync(task.JobType, task.JobId, nodeTaskResultKey, token);

                var rawResult = new StringBuilder();
                using (var monitor = string.IsNullOrEmpty(cmd) ? null : this.Monitor.StartMonitorTask(taskKey, async (output, cancellationToken) =>
                {
                    try
                    {
                        if (rawResult.Length < MaxRawResultLength)
                        {
                            rawResult.Append(output);
                        }

                        await taskResultBlob.AppendTextAsync(output, Encoding.UTF8, null, null, null, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        this.Logger.LogError(ex, "Error happened when append to blob {0}", taskResultBlob.Name);
                    }
                }))
                {
                    this.Logger.LogInformation("Call startjobandtask for task {0}", taskKey);
                    this.jobsPartitionName = this.Utilities.GetJobPartitionKey(task.JobType, task.JobId);
                    this.nodesPartitionName = this.Utilities.GetNodePartitionKey(nodeName);

                    var taskResultArgs = new ComputeNodeTaskCompletionEventArgs(nodeName, task.JobId, null)
                    {
                        State = TaskState.Dispatching,
                        CustomizedData = task.CustomizedData,
                    };

                    if (!await this.PersistTaskResult(nodeTaskResultKey, taskResultArgs, token)) { return false; }
                    if (!await this.PersistTaskResult(taskResultKey, taskResultArgs, token)) { return false; }

                    // TODO: timeout the dispatch
                    await this.communicator.StartJobAndTaskAsync(
                         nodeName,
                         new StartJobAndTaskArg(new int[0], task.JobId, task.Id), task.UserName, task.Password,
                         new Common.ProcessStartInfo(cmd, "", "", $"{this.communicator.Options.AgentUriBase}/output/{taskKey}",
                            "", task.EnvironmentVariables, new long[0], task.RequeueCount),
                         task.PrivateKey, task.PublicKey, token);

                    // TODO: deal dispatch failure.
                    taskResultArgs.State = TaskState.Running;
                    if (!await this.PersistTaskResult(nodeTaskResultKey, taskResultArgs, token)) { return false; }
                    if (!await this.PersistTaskResult(taskResultKey, taskResultArgs, token)) { return false; }

                    this.Logger.LogInformation("Wait for response for job {0}, task {1}", task.JobId, taskKey);
                    taskResultArgs = await (monitor?.Execution ?? Task.FromResult(taskResultArgs));
                    taskResultArgs.CustomizedData = task.CustomizedData;

                    this.Logger.LogInformation("Saving result for job {0}, task {1}", task.JobId, taskKey);

                    taskResultArgs.State = TaskState.Finished;
                    if (!string.IsNullOrEmpty(cmd) && taskResultArgs.TaskInfo != null)
                    {
                        taskResultArgs.TaskInfo.Message = rawResult.Length > MaxRawResultLength ? rawResult.ToString(0, MaxRawResultLength) : rawResult.ToString();
                    }

                    if (!await this.PersistTaskResult(nodeTaskResultKey, taskResultArgs, token)) { return false; }
                    if (!await this.PersistTaskResult(taskResultKey, taskResultArgs, token)) { return false; }

                    var queue = await this.Utilities.GetOrCreateTaskCompletionQueueAsync(token);
                    await queue.AddMessageAsync(new CloudQueueMessage(JsonConvert.SerializeObject(new TaskCompletionMessage()
                    {
                        JobId = task.JobId,
                        Id = task.Id,
                        ExitCode = taskResultArgs.TaskInfo?.ExitCode,
                        JobType = task.JobType,
                        RequeueCount = task.RequeueCount,
                    }, Formatting.Indented)), null, null, null, null, token);
                }

                return true;
            }
        }
    }
}
