namespace Microsoft.HpcAcm.Services.JobMonitor
{
    using Microsoft.HpcAcm.Common.Dto;
    using Microsoft.HpcAcm.Services.Common;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using T = System.Threading.Tasks;

    class ClusrunJobHandler : ServerObject, IJobTypeHandler
    {
        public T.Task<List<InternalTask>> GenerateTasksAsync(Job job, CancellationToken token)
        {
            return T.Task.FromResult(Enumerable.Range(1, job.TargetNodes.Length).Select(id =>
            {
                var t = InternalTask.CreateFrom(job);
                t.CustomizedData = t.Node = job.TargetNodes[id - 1];
                t.Id = id;
                return t;
            }).ToList());
        }

        public T.Task<string> AggregateTasksAsync(Job job, List<Task> tasks, List<ComputeClusterTaskInformation> taskResults, CancellationToken token)
        {
            var groups = tasks.GroupBy(t => t.State).Select(g => new { State = g.Key, Count = g.Count() });

            return T.Task.FromResult(JsonConvert.SerializeObject(groups));
        }
    }
}
