namespace Microsoft.HpcAcm.Services.JobDispatcher
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
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    public class DiagnosticsJobDispatcher : ServerObject, IDispatcher
    {
        public JobType RestrictedJobType { get => JobType.Diagnostics; }
        public async Task DispatchAsync(Job job, CancellationToken token)
        {
            Debug.Assert(job.Type == this.RestrictedJobType, "Job type mismatch");

            // TODO: github integration
            var jobTable = await this.Utilities.GetOrCreateJobsTableAsync(token);

            var result = await jobTable.ExecuteAsync(TableOperation.Retrieve<JsonTableEntity>(job.DiagnosticTest.Category, job.DiagnosticTest.Name), null, null, token);

            if (!result.IsSuccessfulStatusCode())
            {
                this.Logger.LogError("No diag test found");
                throw new InvalidOperationException("no diag test found");
            }

            if (result.Result is JsonTableEntity entity)
            {
                var diagTest = entity.GetObject<InternalDiagnosticsTest>();

                // Fake output
                List<InternalTask> tasksFake = new List<InternalTask>()
                {
                    new InternalTask() { ChildrenIds = new List<int>() { 1 }, CommandLine = "hostname", Id = 0, JobId = job.Id, JobType = job.Type,
                     Node = "evanc6", ParentsIds = null, RemainingParentIds = null, RequeueCount = job.RequeueCount },
                    new InternalTask() { ChildrenIds = new List<int>() {  }, CommandLine = "hostname", Id = 1, JobId = job.Id, JobType = job.Type,
                     Node = "evanc6", ParentsIds = new List<int>(){0 }, RemainingParentIds = new HashSet<int>(){ 0 }, RequeueCount = job.RequeueCount },
                };

                var dispatchTasks = await PythonExecutor.ExecuteAsync(diagTest.DispatchScript, tasksFake);

                if (!string.IsNullOrEmpty(dispatchTasks.Item2))
                {
                    this.Logger.LogError("Dispatch failed");
                    throw new InvalidOperationException(dispatchTasks.Item2);
                }
                else
                {
                    var tasks = JsonConvert.DeserializeObject<List<InternalTask>>(dispatchTasks.Item1);

                    var batch = new TableBatchOperation();
                    foreach (var e in tasks.Select(t => new JsonTableEntity(
                         this.Utilities.GetJobPartitionKey(job.Type.ToString(), job.Id),
                         this.Utilities.GetTaskKey(job.Id, t.Id, job.RequeueCount),
                         t)))
                    {
                        batch.InsertOrReplace(e);
                    }

                    var tableResults = await jobTable.ExecuteBatchAsync(batch, null, null, token);

                    if (!tableResults.All(r => r.IsSuccessfulStatusCode()))
                    {
                        throw new InvalidOperationException("Not all tasks dispatched successfully");
                    }

                    var taskCompletionQueue = await this.Utilities.GetOrCreateTaskCompletionQueueAsync(token);
                    await taskCompletionQueue.AddMessageAsync(new WindowsAzure.Storage.Queue.CloudQueueMessage(
                        JsonConvert.SerializeObject(new TaskCompletionMessage() { JobId = job.Id, JobType = this.RestrictedJobType, RequeueCount = job.RequeueCount })),
                        null, null, null, null, token);
                }
            }
            else
            {
                this.Logger.LogError("No diag test found");
            }
        }
    }
}
