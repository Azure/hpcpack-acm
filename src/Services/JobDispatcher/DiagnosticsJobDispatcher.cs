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

        private (bool, string) FillData(IEnumerable<InternalTask> tasks, Job job)
        {
            var tasksDict = tasks.ToDictionary(t => t.Id);
            foreach (var t in tasks)
            {
                if (t.ParentIds != null)
                {
                    foreach (var parentId in t.ParentIds)
                    {
                        if (tasksDict.TryGetValue(parentId, out InternalTask p))
                        {
#pragma warning disable S1121 // Assignments should not be made from within sub-expressions
                            (p.ChildIds ?? (p.ChildIds = new List<int>())).Add(t.Id);
#pragma warning restore S1121 // Assignments should not be made from within sub-expressions
                        }
                        else
                        {
                            return (false, $"Task {t.Id}'s parent {parentId} not found.");
                        }
                    }
                }

                t.RemainingParentIds = t.ParentIds?.ToHashSet();
                t.JobId = job.Id;
                t.JobType = job.Type;
                t.RequeueCount = job.RequeueCount;
            }

            return (true, null);
        }

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

                var dispatchTasks = await PythonExecutor.ExecuteAsync(diagTest.DispatchScript, job);

                if (!string.IsNullOrEmpty(dispatchTasks.Item2))
                {
                    this.Logger.LogError("Dispatch failed");
                    throw new InvalidOperationException(dispatchTasks.Item2);
                }
                else
                {
                    var tasks = JsonConvert.DeserializeObject<List<InternalTask>>(dispatchTasks.Item1);

                    var (success, msg) = this.FillData(tasks, job);
                    if (!success)
                    {
                        this.Logger.LogError(msg);
                        // fail the job.
                    }

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
