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
            string jobPartitionKey,
            int jobId,
            int jobRequeueCount,
            int taskId,
            Func<InternalTask, Task> action,
            CancellationToken token)
        {
            var result = await this.jobsTable.ExecuteAsync(
                TableOperation.Retrieve<JsonTableEntity>(
                    jobPartitionKey,
                    this.Utilities.GetTaskKey(jobId, taskId, jobRequeueCount)),
                null,
                null,
                token);

            this.Logger.LogInformation("Queried job table for task id {0}, result {1}", taskId, result.HttpStatusCode);

            if (result.Result is JsonTableEntity entity)
            {
                var internalTask = entity.GetObject<InternalTask>();

                foreach (var childId in internalTask.ChildIds)
                {
                    do
                    {
                        var taskResult = await this.jobsTable.ExecuteAsync(
                            TableOperation.Retrieve<JsonTableEntity>(
                                jobPartitionKey,
                                this.Utilities.GetTaskKey(jobId, childId, jobRequeueCount)),
                            null,
                            null,
                            token);

                        this.Logger.LogInformation("Queried job table for task id {0}, result {1}", taskId, taskResult.HttpStatusCode);

                        if (taskResult.Result is JsonTableEntity taskEntity)
                        {
                            var childTask = taskEntity.GetObject<InternalTask>();
                            childTask.RemainingParentIds.Remove(taskId);

                            if (childTask.RemainingParentIds.Count == 0)
                            {
                                await action(childTask);
                            }

                            taskEntity.PutObject(childTask);
                            var updateResult = await this.jobsTable.ExecuteAsync(TableOperation.InsertOrReplace(taskEntity), null, null, token);

                            // conflict
                            if (updateResult.HttpStatusCode == 409) { continue; }

                            if (!updateResult.IsSuccessfulStatusCode())
                            {
                                await this.Utilities.UpdateJobAsync(jobPartitionKey, j =>
                                {
                                    j.State = JobState.Failed;
                                    (j.Events ?? (j.Events = new List<Event>())).Add(new Event()
                                    {
                                        Content = $"Unable to update task record {childId}, status {updateResult.HttpStatusCode}",
                                        Source = EventSource.Job,
                                        Type = EventType.Alert
                                    });
                                }, token);
                            }
                        }
                        else
                        {
                            Debug.Assert(false, "not JsonTableEntity type");
                        }
                    }
                    while (false);
                }
            }
            else
            {
                Debug.Assert(false, "not JsonTableEntity type");
            }
        }

        public override async Task<bool> ProcessTaskItemAsync(TaskItem taskItem, CancellationToken token)
        {
            var message = taskItem.GetMessage<TaskCompletionMessage>();
            using (this.Logger.BeginScope("Do work for TaskCompletionMessage {0}", message.Id))
            {
                var jobPartitionKey = this.Utilities.GetJobPartitionKey(message.JobType, message.JobId);

                var job = await this.Utilities.GetJobsTable().RetrieveAsync<Job>(jobPartitionKey, this.Utilities.JobEntryKey, token);
                if (job != null && job.State == JobState.Running)
                {
                    await this.VisitAllTasksAsync(jobPartitionKey, message.JobId, message.RequeueCount, message.Id, async t =>
                    {
                        if (string.Equals(t.CustomizedData, InternalTask.EndTaskMark, StringComparison.OrdinalIgnoreCase))
                        {
                            await this.Utilities.UpdateJobAsync(jobPartitionKey, j => j.State = j.State == JobState.Running ? JobState.Finishing : j.State, token);
                            var jobEventQueue = await this.Utilities.GetOrCreateJobEventQueueAsync(token);
                            await jobEventQueue.AddMessageAsync(
                                new CloudQueueMessage(JsonConvert.SerializeObject(new JobEventMessage() { Id = job.Id, EventVerb = "finish", Type = job.Type })),
                                null, null, null, null,
                                token);

                            return;
                        }

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
