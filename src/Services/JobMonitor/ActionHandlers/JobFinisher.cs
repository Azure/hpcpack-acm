namespace Microsoft.HpcAcm.Services.JobMonitor
{
    using Microsoft.HpcAcm.Common.Dto;
    using Microsoft.HpcAcm.Common.Utilities;
    using Microsoft.HpcAcm.Services.Common;
    using Microsoft.WindowsAzure.Storage.Queue;
    using Microsoft.WindowsAzure.Storage.Table;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using T = System.Threading.Tasks;

    class JobFinisher : JobActionHandlerBase
    {
        public override async T.Task ProcessAsync(Job job, JobEventMessage message, CancellationToken token)
        {
            var jobTable = this.Utilities.GetJobsTable();

            if (job.State != JobState.Finishing)
            {
                return;
            }

            var jobPartitionKey = this.Utilities.GetJobPartitionKey(job.Type, job.Id);

            var jobPartitionQuery = this.Utilities.GetPartitionQueryString(jobPartitionKey);
            var taskResultRangeQuery = this.Utilities.GetRowKeyRangeString(
                this.Utilities.GetTaskResultKey(job.Id, 0, job.RequeueCount),
                this.Utilities.GetTaskResultKey(job.Id, int.MaxValue, job.RequeueCount));
            var allTaskResults = (await jobTable.QueryAsync<ComputeClusterTaskInformation>(
                TableQuery.CombineFilters(jobPartitionQuery, TableOperators.And, taskResultRangeQuery),
                null,
                token))
                .Select(t => t.Item3)
                .ToList();

            var taskRangeQuery = this.Utilities.GetRowKeyRangeString(
                this.Utilities.GetTaskKey(job.Id, 0, job.RequeueCount),
                this.Utilities.GetTaskKey(job.Id, int.MaxValue, job.RequeueCount));
            var allTasks = (await jobTable.QueryAsync<Task>(
                TableQuery.CombineFilters(jobPartitionQuery, TableOperators.And, taskRangeQuery),
                null,
                token))
                .Select(t => t.Item3)
                .Where(t => t.CustomizedData != Task.EndTaskMark)
                .ToList();

            await this.JobTypeHandler.AggregateTasksAsync(job, allTasks, allTaskResults, token);

            if (job.State == JobState.Finishing)
            {
                var finalState = job.State == JobState.Finishing ? JobState.Finished : JobState.Canceled;
                if (job.FailJobOnTaskFailure && allTasks.Any(t => t.State == TaskState.Failed))
                {
                    finalState = JobState.Failed;
                }

                job.State = finalState;
            }

            await this.Utilities.UpdateJobAsync(job.Type, job.Id, j =>
            {
                j.State = job.State;
                j.AggregationResult = job.AggregationResult;
                j.Events = job.Events;
            }, token);

            foreach(var task in allTasks.Where(t => t.CustomizedData != Task.EndTaskMark))
            {
                var q = await this.Utilities.GetOrCreateNodeCancelQueueAsync(task.Node, token);
                await q.AddMessageAsync(new CloudQueueMessage(
                    JsonConvert.SerializeObject(new TaskEventMessage() { JobId = job.Id, Id = task.Id, JobType = job.Type, RequeueCount = job.RequeueCount, EventVerb = "cancel" })),
                    null, null, null, null, token);
            }
        }
    }
}
