namespace Microsoft.HpcAcm.Services.JobMonitor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.HpcAcm.Common.Dto;
    using Microsoft.HpcAcm.Common.Utilities;
    using Microsoft.HpcAcm.Services.Common;
    using Newtonsoft.Json;

    public class DiagnosticsJobFinisher : JobFinisher
    {
        public override JobType RestrictedJobType => JobType.Diagnostics;
        public override async Task AggregateTasksAsync(Job job, List<ComputeNodeTaskCompletionEventArgs> taskResults, CancellationToken token)
        {
            var jobTable = this.Utilities.GetJobsTable();

            var diagTest = await jobTable.RetrieveAsync<InternalDiagnosticsTest>(
                this.Utilities.GetDiagPartitionKey(job.DiagnosticTest.Category),
                job.DiagnosticTest.Name,
                token);

            if (diagTest != null)
            {
                var aggregationResult = await PythonExecutor.ExecuteAsync(diagTest.AggregationScript, job);
                job.AggregationResult = aggregationResult.Item1;
                if (!string.IsNullOrEmpty(aggregationResult.Item2))
                {
                    (job.Events ?? (job.Events = new List<Event>())).Add(new Event()
                    {
                        Content = aggregationResult.Item2,
                        Source = EventSource.Job,
                        Type = EventType.Alert,
                    });

                    job.State = JobState.Failed;
                }
            }
            else
            {
                job.State = JobState.Failed;
                (job.Events ?? (job.Events = new List<Event>())).Add(new Event()
                {
                    Content = $"No diag test {job.DiagnosticTest.Category}/{job.DiagnosticTest.Name} found",
                    Source = EventSource.Job,
                    Type = EventType.Alert
                });
            }
        }
    }
}
