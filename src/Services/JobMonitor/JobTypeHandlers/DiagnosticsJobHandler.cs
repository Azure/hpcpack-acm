namespace Microsoft.HpcAcm.Services.JobMonitor
{
    using Microsoft.HpcAcm.Common.Dto;
    using Microsoft.HpcAcm.Common.Utilities;
    using Microsoft.HpcAcm.Services.Common;
    using Newtonsoft.Json;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using T = System.Threading.Tasks;

    class DiagnosticsJobHandler : ServerObject, IJobTypeHandler
    {
        public async T.Task<DispatchResult> DispatchAsync(Job job, CancellationToken token)
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
                var metadata = await T.Task.WhenAll(job.TargetNodes.Select(async n =>
                {
                    var heartbeatKey = this.Utilities.GetHeartbeatKey(n);
                    var nodeInfo = await nodesTable.RetrieveJsonTableEntityAsync(this.Utilities.NodesPartitionKey, heartbeatKey, token);

                    return new
                    {
                        Node = n,
                        Metadata = await nodesTable.RetrieveAsync<object>(this.Utilities.GetNodePartitionKey(n), metadataKey, token),
                        Heartbeat = nodeInfo?.GetObject<ComputeClusterNodeInformation>(),
                        LastHeartbeatTime = nodeInfo?.Timestamp,
                        NodeRegistrationInfo = await nodesTable.RetrieveAsync<ComputeClusterRegistrationInformation>(this.Utilities.NodesPartitionKey, this.Utilities.GetRegistrationKey(n), token),
                    };
                }));

                var dispatchTasks = await PythonExecutor.ExecuteAsync(scriptBlob, new { Job = job, Nodes = metadata }, token);

                if (dispatchTasks.IsError)
                {
                    this.Logger.Error("Generate tasks, job {0}, {1}", job.Id, dispatchTasks.ErrorMessage);

                    await this.Utilities.FailJobWithEventAsync(
                        job,
                        $"Dispatch script {dispatchTasks.ErrorMessage}",
                        token);

                    return null;
                }
                else
                {
                    return JsonConvert.DeserializeObject<DispatchResult>(dispatchTasks.Output);
                }
            }
            else
            {
                await this.Utilities.FailJobWithEventAsync(
                    job,
                    $"No diag test {job.DiagnosticTest.Category}/{job.DiagnosticTest.Name} found",
                    token);

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

                    job.State = JobState.Failed;
                    await this.Utilities.AddJobsEventAsync(job, $"Diag reduce script {aggregationResult.ErrorMessage}", EventType.Alert, token);
                }
            }
            else
            {
                job.State = JobState.Failed;
                await this.Utilities.AddJobsEventAsync(job, $"No diag test {job.DiagnosticTest.Category}/{job.DiagnosticTest.Name} found", EventType.Alert, token);
            }

            return result;
        }
    }
}
