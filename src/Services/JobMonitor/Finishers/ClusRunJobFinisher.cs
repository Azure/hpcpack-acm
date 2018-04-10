namespace Microsoft.HpcAcm.Services.JobMonitor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.HpcAcm.Common.Dto;
    using Newtonsoft.Json;

    public class ClusRunJobFinisher : JobFinisher
    {
        public override JobType RestrictedJobType => JobType.ClusRun;
        public override Task AggregateTasksAsync(Job job, List<ComputeNodeTaskCompletionEventArgs> taskResults, CancellationToken token)
        {
            var groups = taskResults.GroupBy(t => t.State).Select(g => new { State = g.Key, Count = g.Count() });

            job.AggregationResult = JsonConvert.SerializeObject(groups);
            return Task.CompletedTask;
        }
    }
}
