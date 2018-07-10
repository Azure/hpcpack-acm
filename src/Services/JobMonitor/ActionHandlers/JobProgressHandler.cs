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

    class JobProgressHandler : JobActionHandlerBase
    {
        public override T.Task ProcessAsync(Job job, JobEventMessage message, CancellationToken token)
        {
            return T.Task.CompletedTask;
            /*
            var jobTable = this.Utilities.GetJobsTable();

            var jobPartitionKey = this.Utilities.GetJobPartitionKey(job.Type, job.Id);
            var jobPartitionQuery = this.Utilities.GetPartitionQueryString(jobPartitionKey);
            var taskRangeQuery = this.Utilities.GetRowKeyRangeString(
                this.Utilities.GetTaskKey(job.Id, 0, job.RequeueCount),
                this.Utilities.GetTaskKey(job.Id, int.MaxValue, job.RequeueCount));

            var allTasks = (await jobTable.QueryAsync<Task>(
                TableQuery.CombineFilters(jobPartitionQuery, TableOperators.And, taskRangeQuery),
                null,
                token))
                .Select(t => t.Item3)
                .ToList();

            int total = Math.Max(allTasks.Count, 1);

            int ended = allTasks.Count(t => t.State == TaskState.Failed || t.State == TaskState.Finished || t.State == TaskState.Canceled);
            */
          //  await this.Utilities.UpdateJobAsync(job.Type, job.Id, j => j.Progress = 1.0 * ended / total, token);
        }
}
}
