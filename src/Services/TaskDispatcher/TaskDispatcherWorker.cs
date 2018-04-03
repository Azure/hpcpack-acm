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
            int? taskId,
            Func<InternalTask, Task> action,
            CancellationToken token)
        {
            if (!taskId.HasValue)
            {
                var rowKeyRange = this.Utilities.GetRowKeyRangeString(this.Utilities.GetMinimumTaskKey(jobId, jobRequeueCount), this.Utilities.GetMaximumTaskKey(jobId, jobRequeueCount));
                var partitionQuery = this.Utilities.GetPartitionQueryString(jobPartitionKey);

                var q = TableQuery.CombineFilters(partitionQuery, TableOperators.And, rowKeyRange);

                TableContinuationToken conToken = null;

                do
                {
                    var result = await this.jobsTable.ExecuteQuerySegmentedAsync(new TableQuery<JsonTableEntity>().Where(q), conToken, null, null, token);
                    await Task.WhenAll(result.Results
                        .Select(r => r.GetObject<InternalTask>())
                        .Where(t => t.RemainingParentIds == null || t.RemainingParentIds.Count == 0)
                        .Select(t => action(t)));

                    conToken = result.ContinuationToken;
                }
                while (conToken != null);
            }
            else
            {
                var result = await this.jobsTable.ExecuteAsync(
                    TableOperation.Retrieve<JsonTableEntity>(
                        jobPartitionKey,
                        this.Utilities.GetTaskKey(jobId, taskId.Value, jobRequeueCount)),
                    null,
                    null,
                    token);

                this.Logger.LogInformation("Queried job table for task id {0}, result {1}", taskId.Value, result.HttpStatusCode);

                if (result.Result is JsonTableEntity entity)
                {
                    var internalTask = entity.GetObject<InternalTask>();
                    foreach (var childId in internalTask.ChildrenIds)
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
                                childTask.RemainingParentIds.Remove(taskId.Value);

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
                                    // log error. fail job
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
        }

        public override async Task ProcessTaskItemAsync(TaskItem taskItem, CancellationToken token)
        {
            var message = taskItem.GetMessage<TaskCompletionMessage>();
            using (this.Logger.BeginScope("Do work for TaskCompletionMessage {0}", message.Id))
            {
                var jobPartitionKey = this.Utilities.GetJobPartitionKey($"{message.JobType}", message.JobId);

                await this.VisitAllTasksAsync(jobPartitionKey, message.JobId, message.RequeueCount, message.Id, async t =>
                {
                    var queue = await this.Utilities.GetOrCreateNodeDispatchQueueAsync(t.Node, token);
                    await queue.AddMessageAsync(
                        new CloudQueueMessage(JsonConvert.SerializeObject(t, Formatting.Indented)),
                        null,
                        null,
                        null,
                        null,
                        token);
                }, token);

                await taskItem.FinishAsync(token);
            }
        }
    }
}
