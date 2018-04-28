namespace Microsoft.HpcAcm.Services.TaskDispatcher
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
    using Microsoft.Extensions.Options;
    using System.Diagnostics;
    using System.IO;

    internal class TaskDispatcherWorker : TaskItemWorker, IWorker
    {
        private readonly TaskDispatcherOptions options;

        public TaskDispatcherWorker(IOptions<TaskDispatcherOptions> options) : base(options.Value)
        {
            this.options = options.Value;
        }

        private CloudTable jobsTable;

        public override async Task InitializeAsync(CancellationToken token)
        {
            this.jobsTable = await this.Utilities.GetOrCreateJobsTableAsync(token);

            this.Source = new QueueTaskItemSource(
                await this.Utilities.GetOrCreateTaskCompletionQueueAsync(token),
                TimeSpan.FromSeconds(this.options.VisibleTimeoutSeconds),
                TimeSpan.FromSeconds(this.options.RetryIntervalSeconds));

            await base.InitializeAsync(token);
        }

        private async Task VisitAllTasksAsync(
            Job job,
            int taskId,
            Func<InternalTask, Task> action,
            CancellationToken token)
        {
            var jobPartitionKey = this.Utilities.GetJobPartitionKey(job.Type, job.Id);
            var internalTask = await this.jobsTable.RetrieveAsync<InternalTask>(
                jobPartitionKey,
                this.Utilities.GetTaskKey(job.Id, taskId, job.RequeueCount),
                token);

            if (job.Type == JobType.Diagnostics &&
                (internalTask.CustomizedData != InternalTask.EndTaskMark && internalTask.CustomizedData != InternalTask.StartTaskMark))
            {
                var diagTest = await this.jobsTable.RetrieveAsync<InternalDiagnosticsTest>(
                    this.Utilities.GetDiagPartitionKey(job.DiagnosticTest.Category),
                    job.DiagnosticTest.Name,
                    token);

                if (diagTest.TaskResultFilterScript?.Name != null)
                {
                    var taskResultKey = this.Utilities.GetTaskResultKey(job.Id, internalTask.Id, job.RequeueCount);
                    var taskResult = await this.jobsTable.RetrieveAsync<ComputeNodeTaskCompletionEventArgs>(jobPartitionKey, taskResultKey, token);
                    var scriptBlob = this.Utilities.GetBlob(diagTest.TaskResultFilterScript.ContainerName, diagTest.TaskResultFilterScript.Name);
                    var fileName = Path.GetTempFileName();

                    var filteredResult = await PythonExecutor.ExecuteAsync(fileName, scriptBlob, new { Job = job, Task = taskResult }, token);
                    taskResult.FilteredResult = filteredResult.Item1 ?? filteredResult.Item2;

                    await this.jobsTable.InsertOrReplaceAsJsonAsync(jobPartitionKey, taskResultKey, taskResult, token);

                    if (!string.IsNullOrEmpty(filteredResult.Item2))
                    {
                        await this.Utilities.UpdateJobAsync(job.Type, job.Id, j =>
                        {
                            (j.Events ?? (j.Events = new List<Event>())).Add(new Event()
                            {
                                Content = filteredResult.Item2,
                                Source = EventSource.Job,
                                Type = EventType.Alert,
                            });

                            j.State = JobState.Failed;
                        }, token);

                        return;
                    }
                }
            }

            foreach (var childId in internalTask.ChildIds)
            {
                do
                {
                    var childTaskKey = this.Utilities.GetTaskKey(job.Id, childId, job.RequeueCount);
                    var childTask = await this.jobsTable.RetrieveAsync<InternalTask>(
                        jobPartitionKey,
                        childTaskKey,
                        token);
                    childTask.RemainingParentIds.Remove(taskId);

                    if (childTask.RemainingParentIds.Count == 0)
                    {
                        await action(childTask);
                    }

                    if (!await this.jobsTable.InsertOrReplaceAsJsonAsync(jobPartitionKey, childTaskKey, childTask, token))
                    {
                        await this.Utilities.UpdateJobAsync(job.Type, job.Id, j =>
                        {
                            j.State = JobState.Failed;
                            (j.Events ?? (j.Events = new List<Event>())).Add(new Event()
                            {
                                Content = $"Unable to update task record {childId}",
                                Source = EventSource.Job,
                                Type = EventType.Alert
                            });
                        }, token);
                    }
                }
                while (false);
            }
        }

        public override async Task<bool> ProcessTaskItemAsync(TaskItem taskItem, CancellationToken token)
        {
            var message = taskItem.GetMessage<TaskCompletionMessage>();
            using (this.Logger.BeginScope("Do work for TaskCompletionMessage {0}", message.Id))
            {
                var jobPartitionKey = this.Utilities.GetJobPartitionKey(message.JobType, message.JobId);

                var job = await this.jobsTable.RetrieveAsync<Job>(jobPartitionKey, this.Utilities.JobEntryKey, token);
                if (job.RequeueCount != message.RequeueCount)
                {
                    this.Logger.LogWarning("The job {0} is already requeued, job requeueCount {1}, message requeueCount {2}", job.Id, job.RequeueCount, message.RequeueCount);
                    return true;
                }

                if (job != null && job.State == JobState.Running)
                {
                    await this.VisitAllTasksAsync(job, message.Id, async t =>
                    {
                        if (string.Equals(t.CustomizedData, InternalTask.EndTaskMark, StringComparison.OrdinalIgnoreCase))
                        {
                            await this.Utilities.UpdateJobAsync(job.Type, job.Id, j => j.State = j.State == JobState.Running ? JobState.Finishing : j.State, token);
                            var jobEventQueue = await this.Utilities.GetOrCreateJobEventQueueAsync(token);
                            await jobEventQueue.AddMessageAsync(
                                new CloudQueueMessage(JsonConvert.SerializeObject(new JobEventMessage() { Id = job.Id, EventVerb = "finish", Type = job.Type })),
                                null, null, null, null,
                                token);

                            return;
                        }

                        // TODO: skip the finished tasks.
                        var queue = await this.Utilities.GetOrCreateNodeDispatchQueueAsync(t.Node, token);
                        await queue.AddMessageAsync(
                            new CloudQueueMessage(JsonConvert.SerializeObject(t, Formatting.Indented)),
                            null,
                            null,
                            null,
                            null,
                            token);
                    }, token);
                }

                return true;
            }
        }
    }
}
