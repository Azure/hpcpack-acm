namespace Microsoft.HpcAcm.Services.NodeAgent
{
    using Serilog;
    using Microsoft.HpcAcm.Common.Dto;
    using Microsoft.HpcAcm.Common.Utilities;
    using Microsoft.HpcAcm.Services.Common;
    using Microsoft.WindowsAzure.Storage.Queue;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using T = System.Threading.Tasks;

    public class NodeSynchronizer : ServerObject
    {
        public async T.Task Sync(ComputeClusterNodeInformation nodeInfo, CancellationToken token)
        {
            var jobsTable = this.Utilities.GetJobsTable();
            foreach (var j in nodeInfo.Jobs)
            {
                var job = await jobsTable.RetrieveAsync<Job>(this.Utilities.GetJobPartitionKey(JobType.ClusRun, j.JobId), this.Utilities.JobEntryKey, token)
                    ?? await jobsTable.RetrieveAsync<Job>(this.Utilities.GetJobPartitionKey(JobType.Diagnostics, j.JobId), this.Utilities.JobEntryKey, token);

                if (job == null || job.State == JobState.Canceled || job.State == JobState.Failed || job.State == JobState.Finished)
                {
                    this.Logger.Information("Node {0}, {1} job {2} is reported running, but actually {3} in store.", nodeInfo.Name, job?.Type, j.JobId, job == null ? "null" : job.State.ToString());
                    var q = await this.Utilities.GetOrCreateNodeCancelQueueAsync(this.ServerOptions.HostName, token);

                    // For non-exist job, we don't care about the type, the cancel logic should handle it.
                    await q.AddMessageAsync(new CloudQueueMessage(
                        JsonConvert.SerializeObject(new TaskEventMessage() { JobId = j.JobId, Id = 0, JobType = job?.Type ?? JobType.ClusRun, RequeueCount = 0, EventVerb = "cancel" })),
                        null, null, null, null, token);

                    // cancel the job and tasks
                    foreach (var t in j.Tasks)
                    {
                        this.Logger.Information("Node {0}, {1} job {2}, sending cancel for task {3}.{4}.", nodeInfo.Name, job?.Type, j.JobId, t?.TaskId, t?.TaskRequeueCount);
                        await q.AddMessageAsync(new CloudQueueMessage(
                            JsonConvert.SerializeObject(new TaskEventMessage() { JobId = j.JobId, Id = t.TaskId, JobType = job?.Type ?? JobType.ClusRun, RequeueCount = t.TaskRequeueCount ?? 0, EventVerb = "cancel" })),
                            null, null, null, null, token);
                    }
                }
            }
        }
    }
}
