namespace Microsoft.HpcAcm.Services.NodeAgent
{
    using Serilog;
    using Microsoft.HpcAcm.Services.Common;
    using Microsoft.HpcAcm.Common.Utilities;
    using Microsoft.HpcAcm.Common.Dto;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using T = System.Threading.Tasks;
    using System.Text;
    using Microsoft.WindowsAzure.Storage.Queue;
    using Newtonsoft.Json;
    using Microsoft.WindowsAzure.Storage.Table;
    using Microsoft.WindowsAzure.Storage.Blob;
    using Microsoft.WindowsAzure.Storage;

    public class StartJobAndTaskProcessor : JobTaskProcessor
    {
        private string nodePartitionKey;
        private string jobPartitionKey;
        private const int MaxRawResultLength = 4096;

        private CloudTable jobsTable;
        private CloudTable nodesTable;

        private TaskMonitor Monitor { get; }

        private ILogger logger;

        public StartJobAndTaskProcessor(TaskMonitor monitor, NodeCommunicator communicator) : base(communicator)
        {
            this.Monitor = monitor;
        }

        private async T.Task<bool> PersistTaskResult(string resultKey, object result, CancellationToken token)
        {
            var results = await T.Task.WhenAll(
                this.jobsTable.InsertOrReplaceAsync(this.jobPartitionKey, resultKey, result, token),
                this.nodesTable.InsertOrReplaceAsync(this.nodePartitionKey, resultKey, result, token));

            var ret = results.All(r => r.IsSuccessfulStatusCode());
            logger.Information("Save task result {0} to nodes table, {1}", resultKey, ret);
            return ret;
        }

        public override async T.Task<bool> ProcessAsync(TaskEventMessage message, DateTimeOffset? insertionTime, CancellationToken token)
        {
            this.logger = this.Logger.ForContext("Job", message.JobId).ForContext("Task", message.Id);
            this.jobsTable = this.Utilities.GetJobsTable();
            this.nodesTable = this.Utilities.GetNodesTable();
            var nodeName = this.ServerOptions.HostName;

            JobType jobType = message.JobType;
            int jobId = message.JobId;
            int taskId = message.Id;
            int requeueCount = message.RequeueCount;
            this.nodePartitionKey = this.Utilities.GetNodePartitionKey(nodeName);
            this.jobPartitionKey = this.Utilities.GetJobPartitionKey(message.JobType, jobId);
            var taskKey = this.Utilities.GetTaskKey(jobId, taskId, requeueCount);
            var taskInfoKey = this.Utilities.GetTaskInfoKey(jobId, taskId, requeueCount);
            var taskResultKey = this.Utilities.GetTaskResultKey(jobId, taskId, requeueCount);

            logger.Information("Do work {0} for Task {1} on node {2}", message.EventVerb, taskKey, nodeName);

            if (insertionTime != null && insertionTime + TimeSpan.FromSeconds(10) < DateTimeOffset.UtcNow)
            {
                // Only when the insertion time is 10 seconds ago, we check the job status.
                var job = await this.jobsTable.RetrieveAsync<Job>(jobPartitionKey, this.Utilities.JobEntryKey, token);

                if (job.State != JobState.Running)
                {
                    logger.Warning("Trying to start a task {0} when {1} Job {2} is in state {3}", taskKey, job.Type, job.Id, job.State);
                    return true;
                }
            }

            var task = await this.jobsTable.RetrieveAsync<Task>(this.jobPartitionKey, taskKey, token);

            int? exitCode = null;

            CloudAppendBlob taskResultBlob = null;

            var cmd = task.CommandLine;
            DateTimeOffset startTime = DateTimeOffset.UtcNow;
            var taskResult = new ComputeClusterTaskInformation()
            {
                ExitCode = -1,
                Message = "Running",
                CommandLine = cmd,
                JobId = jobId,
                TaskId = taskId,
                NodeName = nodeName,
                ResultKey = taskResultKey,
                StartTime = startTime,
            };

            try
            {
                if (task.State != TaskState.Dispatching && task.State != TaskState.Running && task.State != TaskState.Queued)
                {
                    Logger.Information("Job {0} task {1} state {2}, skip Executing command {3}", jobId, taskId, task.State, cmd);
                    return true;
                }

                var taskInfo = await this.jobsTable.RetrieveAsync<TaskStartInfo>(this.jobPartitionKey, taskInfoKey, token);
                Logger.Information("Executing command {0}", cmd);

                var rawResult = new StringBuilder();
                using (var monitor = string.IsNullOrEmpty(cmd) ? null : this.Monitor.StartMonitorTask(jobId, taskKey, async (output, eof, cancellationToken) =>
                {
                    try
                    {
                        if (rawResult.Length < MaxRawResultLength)
                        {
                            rawResult.Append(output);
                        }

                        taskResultBlob = taskResultBlob ?? await this.Utilities.CreateOrReplaceJobOutputBlobAsync(jobType, taskResultKey, token);
                        await taskResultBlob.AppendTextAsync(output, Encoding.UTF8, null, null, null, cancellationToken);

                        if (eof)
                        {
                            taskResultBlob.Metadata[TaskOutputPage.EofMark] = eof.ToString();
                            await taskResultBlob.SetMetadataAsync(null, null, null, cancellationToken);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, "Error happened when append to blob {0}", taskResultBlob.Name);
                    }
                }))
                {
                    if (!await this.PersistTaskResult(taskResultKey, taskResult, token)) { return false; }

                    logger.Information("Call startjobandtask for task {0}", taskKey);
                    (taskInfo.StartInfo.environmentVariables ?? (taskInfo.StartInfo.environmentVariables = new Dictionary<string, string>()))
                        .Add("blobEndpoint", this.Utilities.Account.BlobEndpoint.AbsoluteUri);
                    taskInfo.StartInfo.stdout = $"{this.Communicator.Options.AgentUriBase}/output/{jobId}/{taskKey}";

                    await this.Utilities.UpdateTaskAsync(jobPartitionKey, taskKey, t => t.State = TaskState.Dispatching, token, this.logger);
                    await this.Communicator.StartJobAndTaskAsync(
                        nodeName,
                        new StartJobAndTaskArg(new int[0], taskInfo.JobId, taskInfo.Id), taskInfo.UserName, taskInfo.Password,
                        taskInfo.StartInfo, taskInfo.PrivateKey, taskInfo.PublicKey, token);


                    logger.Information("Update task state to running");
                    await this.Utilities.UpdateTaskAsync(jobPartitionKey, taskKey, t => t.State = TaskState.Running, token, this.logger);

                    logger.Information("Waiting for response");
                    if (monitor == null) return true;

                    ComputeNodeTaskCompletionEventArgs taskResultArgs;

                    try
                    {
                        if (monitor.Result.Execution == await T.Task.WhenAny(monitor.Result.Execution, T.Task.Delay(TimeSpan.FromSeconds(task.MaximumRuntimeSeconds))))
                        {
                            taskResultArgs = monitor.Result.Execution.Result;
                        }
                        else
                        {
                            logger.Information("Task has timed out");
                            return true;
                        }
                    }
                    catch (AggregateException ex) when (ex.InnerExceptions.All(e => e is OperationCanceledException))
                    {
                        logger.Information("Task has been canceled");
                        return true;
                    }
                    catch (T.TaskCanceledException)
                    {
                        logger.Information("Task has been canceled");
                        return true;
                    }

                    taskResult = taskResultArgs.TaskInfo ?? taskResult;
                    logger.Information("Updating task state with exit code {0}", taskResult?.ExitCode);
                    await this.Utilities.UpdateTaskAsync(jobPartitionKey, taskKey,
                        t => t.State = taskResult?.ExitCode == 0 ? TaskState.Finished : TaskState.Failed,
                        token,
                        this.logger);

                    if (taskResult != null)
                    {
                        taskResult.StartTime = startTime;
                        exitCode = taskResult.ExitCode;
                        taskResult.Message = rawResult.Length > MaxRawResultLength ? rawResult.ToString(0, MaxRawResultLength) : rawResult.ToString();
                        taskResult.CommandLine = cmd;
                        taskResult.JobId = jobId;
                        taskResult.TaskId = taskId;
                        taskResult.NodeName = nodeName;
                        taskResult.ResultKey = taskResultKey;
                        taskResult.EndTime = DateTimeOffset.UtcNow;

                        logger.Information("Saving result");
                        if (!await this.PersistTaskResult(taskResultKey, taskResult, token)) { return false; }
                    }
                }
            }
            catch (StorageException ex) when (ex.IsCancellation()) { return false; }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { return false; }
            catch (Exception ex)
            {
                taskResult.Message = ex.ToString();
                taskResult.EndTime = DateTimeOffset.UtcNow;
                await this.PersistTaskResult(taskResultKey, taskResult, token);
                await this.Utilities.UpdateTaskAsync(jobPartitionKey, taskKey, t => t.State = t.State == TaskState.Dispatching || t.State == TaskState.Running ? TaskState.Failed : t.State, token, this.logger);
                await this.Utilities.UpdateJobAsync(jobType, jobId, j =>
                {
                    (j.Events ?? (j.Events = new List<Event>())).Add(new Event()
                    {
                        Type = EventType.Warning,
                        Source = EventSource.Job,
                        Content = $"Task {taskId}, exception {ex}",
                    });
                }, token, this.logger);
            }
            finally
            {
                var queue = this.Utilities.GetJobTaskCompletionQueue(jobId);
                logger.Information("Adding task completion message");
                await queue.AddMessageAsync(new CloudQueueMessage(JsonConvert.SerializeObject(new TaskCompletionMessage()
                {
                    JobId = jobId,
                    Id = taskId,
                    ExitCode = exitCode,
                    JobType = jobType,
                    RequeueCount = requeueCount,
                    ChildIds = task.ChildIds,
                }, Formatting.Indented)), null, null, null, null, token);

                taskResultBlob = taskResultBlob ?? await this.Utilities.CreateOrReplaceJobOutputBlobAsync(jobType, taskResultKey, token);
                taskResultBlob.Metadata[TaskOutputPage.EofMark] = true.ToString();
                await taskResultBlob.SetMetadataAsync(null, null, null, token);

                logger.Information("Finished");
            }

            return true;
        }
    }
}
