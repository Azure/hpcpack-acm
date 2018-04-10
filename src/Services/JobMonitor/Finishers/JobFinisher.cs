namespace Microsoft.HpcAcm.Services.JobMonitor
{
    using Microsoft.HpcAcm.Common.Dto;
    using Microsoft.HpcAcm.Common.Utilities;
    using Microsoft.HpcAcm.Services.Common;
    using Microsoft.WindowsAzure.Storage.Table;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    public abstract class JobFinisher : ServerObject, IJobEventProcessor
    {
        public abstract JobType RestrictedJobType { get; }
        public string EventVerb { get => "finish"; }

        public async Task ProcessAsync(Job job, JobEventMessage message, CancellationToken token)
        {
            Debug.Assert(job.Type == this.RestrictedJobType, "Job type mismatch");

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

            var allTasks = (await jobTable.QueryAsync<ComputeNodeTaskCompletionEventArgs>(
                TableQuery.CombineFilters(jobPartitionQuery, TableOperators.And, taskResultRangeQuery),
                null,
                token))
                .Select(t => t.Item3)
                .ToList();


            await this.AggregateTasksAsync(job, allTasks, token);

            if (job.State == JobState.Finishing)
            {
                var finalState = job.State == JobState.Finishing ? JobState.Finished : JobState.Canceled;
                if (job.FailJobOnTaskFailure && allTasks.Any(t => t.State == TaskState.Failed))
                {
                    finalState = JobState.Failed;
                }

                job.State = finalState;
            }

            await this.Utilities.UpdateJobAsync(jobPartitionKey, j =>
            {
                j.State = job.State;
                j.AggregationResult = job.AggregationResult;
                j.Events = job.Events;
            }, token);
        }

        public abstract Task AggregateTasksAsync(Job job, List<ComputeNodeTaskCompletionEventArgs> taskResults, CancellationToken token);
    }
}
