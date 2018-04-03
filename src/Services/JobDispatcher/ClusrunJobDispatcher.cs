namespace Microsoft.HpcAcm.Services.JobDispatcher
{
    using Microsoft.HpcAcm.Common.Dto;
    using Microsoft.HpcAcm.Common.Utilities;
    using Microsoft.HpcAcm.Services.Common;
    using Microsoft.WindowsAzure.Storage.Queue;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    public class ClusrunJobDispatcher : ServerObject, IDispatcher
    {
        public JobType RestrictedJobType { get => JobType.ClusRun; }

        public async Task DispatchAsync(Job job, CancellationToken token)
        {
            Debug.Assert(job.Type == this.RestrictedJobType, "Job type mismatch");

            var internalTask = InternalTask.CreateFrom(job);

            await Task.WhenAll(job.TargetNodes.Select(async n =>
            {
                var q = await this.Utilities.GetOrCreateNodeDispatchQueueAsync(n, token);
                internalTask.Node = n;
                await q.AddMessageAsync(new CloudQueueMessage(JsonConvert.SerializeObject(internalTask)), null, null, null, null, token);
            }));
        }
    }
}
