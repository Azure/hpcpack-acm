namespace Microsoft.HpcAcm.Services.JobMonitor
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
    using T = System.Threading.Tasks;

    public class ClusRunJobDispatcher : JobDispatcher
    {
        public override JobType RestrictedJobType { get => JobType.ClusRun; }

        public override T.Task<List<InternalTask>> GenerateTasksAsync(Job job, CancellationToken token)
        {
            return T.Task.FromResult(Enumerable.Range(1, job.TargetNodes.Length).Select(id =>
            {
                var t = InternalTask.CreateFrom(job);
                t.CustomizedData = t.Node = job.TargetNodes[id - 1];
                t.Id = id;
                return t;
            }).ToList());
        }
    }
}
