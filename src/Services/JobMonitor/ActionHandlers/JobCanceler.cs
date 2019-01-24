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
    using System.Net;
    using System.Text;
    using System.Threading;
    using T = System.Threading.Tasks;

    class JobCanceler : JobActionHandlerBase
    {
        public override async T.Task ProcessAsync(Job job, JobEventMessage message, CancellationToken token)
        {
            var jobTable = this.Utilities.GetJobsTable();

            if (job.State != JobState.Canceling && job.State != JobState.Running)
            {
                return;
            }

            if (job.State == JobState.Running)
            {
                await this.Utilities.AddJobsEventAsync(job, $"The job maximum execution time {job.MaximumRuntimeSeconds} seconds expired.", EventType.Alert, token);
            }

            await this.Utilities.UpdateJobAsync(job.Type, job.Id, j =>
            {
                j.State = j.State == JobState.Running ? JobState.Canceling : j.State;
            }, token, this.Logger);

            var jobPartitionKey = this.Utilities.GetJobPartitionKey(job.Type, job.Id);

            var jobPartitionQuery = this.Utilities.GetPartitionQueryString(jobPartitionKey);
            var taskRangeQuery = this.Utilities.GetRowKeyRangeString(
                this.Utilities.GetTaskKey(job.Id, 0, job.RequeueCount),
                this.Utilities.GetTaskKey(job.Id, int.MaxValue, job.RequeueCount),
                false,
                false);

            var taskQueue = await this.Utilities.GetOrCreateJobTaskCompletionQueueAsync(job.Id, token);
            var msg1 = new CloudQueueMessage(
                JsonConvert.SerializeObject(new TaskCompletionMessage() { JobId = job.Id, Id = int.MaxValue, ExitCode = 0 }));

            await taskQueue.AddMessageAsync(msg1, null, null, null, null, token);
            this.Logger.Information("Added task cancel to queue {0}, {1}", taskQueue.Name, msg1.Id);

            await T.Task.WhenAll(job.TargetNodes.Select(async n =>
             {
                 var q = this.Utilities.GetNodeCancelQueue(n);
                 var msg = new CloudQueueMessage(
                     JsonConvert.SerializeObject(new TaskEventMessage() { JobId = job.Id, Id = 0, JobType = job.Type, RequeueCount = job.RequeueCount, EventVerb = "cancel" }));
                 await q.AddMessageAsync(msg, null, null, null, null, token);
                 this.Logger.Information("Added job {0} cancel to queue {1}, {2}", job.Id, q.Name, msg.Id);
             }));

            await this.Utilities.UpdateJobAsync(job.Type, job.Id, j => j.State = JobState.Canceled, token, this.Logger);
        }
    }
}
