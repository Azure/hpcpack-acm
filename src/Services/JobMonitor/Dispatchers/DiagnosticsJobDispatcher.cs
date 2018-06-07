namespace Microsoft.HpcAcm.Services.JobMonitor
{
    using Microsoft.Extensions.Logging;
    using Microsoft.HpcAcm.Common.Dto;
    using Microsoft.HpcAcm.Common.Utilities;
    using Microsoft.HpcAcm.Services.Common;
    using Microsoft.WindowsAzure.Storage.Table;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using T = System.Threading.Tasks;

    public class DiagnosticsJobDispatcher : JobDispatcher
    {
        public override JobType RestrictedJobType { get => JobType.Diagnostics; }

        public override async T.Task<List<InternalTask>> GenerateTasksAsync(Job job, CancellationToken token)
        {
            // TODO: github integration
            var jobTable = this.Utilities.GetJobsTable();

            var diagTest = await jobTable.RetrieveAsync<InternalDiagnosticsTest>(this.Utilities.GetDiagPartitionKey(job.DiagnosticTest.Category), job.DiagnosticTest.Name, token);

            if (diagTest != null)
            {
                var scriptBlob = this.Utilities.GetBlob(diagTest.DispatchScript.ContainerName, diagTest.DispatchScript.Name);

                var nodesTable = this.Utilities.GetNodesTable();
                var metadataKey = this.Utilities.GetMetadataKey();

                var metadata = await T.Task.WhenAll(job.TargetNodes.Select(async n => new { Node = n, Metadata = await nodesTable.RetrieveAsync<object>(this.Utilities.GetNodePartitionKey(n), metadataKey, token) }));

                var dispatchTasks = await PythonExecutor.ExecuteAsync(scriptBlob, new { Job = job, Nodes = metadata }, token);

                if (dispatchTasks.Item1 != 0)
                {
                    await this.Utilities.UpdateJobAsync(job.Type, job.Id, j =>
                    {
                        j.State = JobState.Failed;
                        (j.Events ?? (j.Events = new List<Event>())).Add(new Event()
                        {
                            Content = $"Dispatch script exit code {dispatchTasks.Item1}, message {dispatchTasks.Item3}",
                            Source = EventSource.Job,
                            Type = EventType.Alert
                        });
                    }, token);

                    this.Logger.LogError("Dispatch failed {0}, {1}", dispatchTasks.Item1, dispatchTasks.Item3);
                    return null;
                }
                else
                {
                    return JsonConvert.DeserializeObject<List<InternalTask>>(dispatchTasks.Item2);
                }
            }
            else
            {
                await this.Utilities.UpdateJobAsync(job.Type, job.Id, j =>
                {
                    j.State = JobState.Failed;
                    (j.Events ?? (j.Events = new List<Event>())).Add(new Event()
                    {
                        Content = $"No diag test {job.DiagnosticTest.Category}/{job.DiagnosticTest.Name} found",
                        Source = EventSource.Job,
                        Type = EventType.Alert
                    });
                }, token);

                this.Logger.LogError("No diag test found");
                return null;
            }
        }
    }
}
