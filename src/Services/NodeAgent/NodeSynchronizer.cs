namespace Microsoft.HpcAcm.Services.NodeAgent
{
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

                if (job.State == JobState.Canceled || job.State == JobState.Failed || job.State == JobState.Finished)
                {
                    // cancel the job and tasks
                    foreach (var t in j.Tasks)
                    {
                        var q = await this.Utilities.GetOrCreateNodeCancelQueueAsync(this.ServerOptions.HostName, token);
                        await q.AddMessageAsync(new CloudQueueMessage(
                            JsonConvert.SerializeObject(new TaskEventMessage() { JobId = j.JobId, Id = t.TaskId, JobType = job.Type, RequeueCount = t.TaskRequeueCount ?? 0, EventVerb = "cancel" })),
                            null, null, null, null, token);
                    }
                }
            }
        }
    }
}
