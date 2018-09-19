namespace Microsoft.HpcAcm.Services.JobMonitor
{
    using Microsoft.HpcAcm.Common.Dto;
    using Microsoft.HpcAcm.Common.Utilities;
    using Microsoft.HpcAcm.Services.Common;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using T = System.Threading.Tasks;

    class DiagnosticsJobHandler : ServerObject, IJobTypeHandler
    {
        public async T.Task<List<InternalTask>> GenerateTasksAsync(Job job, CancellationToken token)
        {
            // TODO: github integration
            var jobTable = this.Utilities.GetJobsTable();

            this.Logger.Information("Generating tasks for job {0}", job.Id);
            var diagTest = await jobTable.RetrieveAsync<InternalDiagnosticsTest>(this.Utilities.GetDiagPartitionKey(job.DiagnosticTest.Category), job.DiagnosticTest.Name, token);

            if (diagTest != null)
            {
                var scriptBlob = this.Utilities.GetBlob(diagTest.DispatchScript.ContainerName, diagTest.DispatchScript.Name);

                var nodesTable = this.Utilities.GetNodesTable();
                var metadataKey = this.Utilities.GetMetadataKey();

                this.Logger.Information("GenerateTasks, Querying node info for job {0}", job.Id);
                var metadata = await T.Task.WhenAll(job.TargetNodes.Select(async n => new
                {
                    Node = n,
                    Metadata = await nodesTable.RetrieveAsync<object>(this.Utilities.GetNodePartitionKey(n), metadataKey, token),
                    NodeRegistrationInfo = await nodesTable.RetrieveAsync<ComputeClusterRegistrationInformation>(this.Utilities.NodesPartitionKey, this.Utilities.GetRegistrationKey(n), token),
                }));

                var dispatchTasks = await PythonExecutor.ExecuteAsync(scriptBlob, new { Job = job, Nodes = metadata }, token);

                if (dispatchTasks.IsError)
                {
                    this.Logger.Error("Generate tasks, job {0}, {1}", job.Id, dispatchTasks.ErrorMessage);
                    await this.Utilities.UpdateJobAsync(job.Type, job.Id, j =>
                    {
                        j.State = JobState.Failed;
                        (j.Events ?? (j.Events = new List<Event>())).Add(new Event()
                        {
                            Content = $"Dispatch script {dispatchTasks.ErrorMessage}",
                            Source = EventSource.Job,
                            Type = EventType.Alert
                        });
                    }, token, this.Logger);

                    return null;
                }
                else
                {
                    return JsonConvert.DeserializeObject<List<InternalTask>>(dispatchTasks.Output);
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
                }, token, this.Logger);

                this.Logger.Error("No diag test found");
                return null;
            }
        }

        public async T.Task<string> AggregateTasksAsync(Job job, List<Task> tasks, List<ComputeClusterTaskInformation> taskResults, CancellationToken token)
        {
            var jobTable = this.Utilities.GetJobsTable();
            this.Logger.Information("AggregateTasks, job {0}", job.Id);

            var diagTest = await jobTable.RetrieveAsync<InternalDiagnosticsTest>(
                this.Utilities.GetDiagPartitionKey(job.DiagnosticTest.Category),
                job.DiagnosticTest.Name,
                token);

            string result = string.Empty;

            if (diagTest != null)
            {
                var scriptBlob = this.Utilities.GetBlob(diagTest.AggregationScript.ContainerName, diagTest.AggregationScript.Name);

                var aggregationResult = await PythonExecutor.ExecuteAsync(scriptBlob, new { Job = job, Tasks = tasks, TaskResults = taskResults }, token);
                result = aggregationResult.Output;
                if (aggregationResult.IsError)
                {
                    this.Logger.Error("AggregateTasks, job {0}, {1}", job.Id, aggregationResult.ErrorMessage);
                    (job.Events ?? (job.Events = new List<Event>())).Add(new Event()
                    {
                        Content = $"Diag reduce script {aggregationResult.ErrorMessage}",
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

            return result;
        }
    }
}
