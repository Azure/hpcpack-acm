namespace Microsoft.HpcAcm.Services.TaskDispatcher
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Microsoft.HpcAcm.Services.Common;
    using Microsoft.HpcAcm.Common.Dto;
    using System.Threading;
    using T = System.Threading.Tasks;
    using Serilog;
    using Microsoft.HpcAcm.Common.Utilities;
    using Microsoft.WindowsAzure.Storage.Table;
    using Microsoft.WindowsAzure.Storage.Queue;
    using Newtonsoft.Json;
    using System.Linq;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Options;
    using System.Diagnostics;
    using System.IO;

    internal class TaskDispatcherWorker : TaskItemWorker, IWorker
    {
        private readonly TaskItemSourceOptions options;

        public TaskDispatcherWorker(IOptions<TaskItemSourceOptions> options) : base(options.Value)
        {
            this.options = options.Value;
        }

        private CloudTable jobsTable;

        public override async T.Task InitializeAsync(CancellationToken token)
        {
            this.jobsTable = await this.Utilities.GetOrCreateJobsTableAsync(token);
            await this.Utilities.GetOrCreateJobEventQueueAsync(token);

            this.Source = new QueueMultiTaskItemSource(
                await this.Utilities.GetOrCreateTaskCompletionQueueAsync(token),
                this.options);

            await base.InitializeAsync(token);
        }

        private async T.Task<bool> TaskResultHook(
            Job job,
            int taskId,
            string path,
            CancellationToken token)
        {
            var jobPartitionKey = this.Utilities.GetJobPartitionKey(job.Type, job.Id);
            var taskResultKey = this.Utilities.GetTaskResultKey(job.Id, taskId, job.RequeueCount);
            var taskResult = await this.jobsTable.RetrieveAsync<ComputeNodeTaskCompletionEventArgs>(jobPartitionKey, taskResultKey, token);

            var filteredResult = await PythonExecutor.ExecuteAsync(path, new { Job = job, Task = taskResult }, token);
            taskResult.TaskInfo.FilteredResult = filteredResult.Item1 == 0 ? filteredResult.Item2 : $"Task result filter script exit code {filteredResult.Item1}, message {filteredResult.Item3}";

            await this.jobsTable.InsertOrReplaceAsync(jobPartitionKey, taskResultKey, taskResult, token);

            if (!string.IsNullOrEmpty(filteredResult.Item2))
            {
                await this.Utilities.UpdateJobAsync(job.Type, job.Id, j =>
                {
                    (j.Events ?? (j.Events = new List<Event>())).Add(new Event()
                    {
                        Content = filteredResult.Item2,
                        Source = EventSource.Job,
                        Type = EventType.Alert,
                    });

                    j.State = JobState.Failed;
                }, token);

                return false;
            }

            return true;
        }

        public override async T.Task<bool> ProcessTaskItemAsync(TaskItem taskItem, CancellationToken token)
        {
            var taskItems = taskItem.GetMessage<TaskItem[]>();
            var messages = taskItems.Select(ti => ti.GetMessage<TaskCompletionMessage>());

            var jobGroups = messages.GroupBy(msg => this.Utilities.GetJobPartitionKey(msg.JobType, msg.JobId));

            var results = await T.Task.WhenAll(jobGroups.Select(async jg =>
            {
                var jobPartitionKey = jg.Key;
                this.Logger.Information("Do work for job {0}", jobPartitionKey);
                var job = await this.jobsTable.RetrieveAsync<Job>(jobPartitionKey, this.Utilities.JobEntryKey, token);
                if (job == null)
                {
                    this.Logger.Warning("Skip processing the task completion of {0}. Job is null.", jobPartitionKey);
                    return true;
                }

                var skippedTasks = string.Join(",", jg.Where(msg => msg.RequeueCount != job.RequeueCount).Select(msg => $"{msg.Id}.{msg.RequeueCount}"));
                if (!string.IsNullOrEmpty(skippedTasks))
                {
                    this.Logger.Warning("Skip processing the task completion, job requeueCount {0}, tasks {1}.", job.RequeueCount, skippedTasks);
                }

                var tasks = jg.Where(msg => msg.RequeueCount == job.RequeueCount).ToList();

                var completedCount = tasks.Count(t => t.Id != 0 && t.Id != int.MaxValue);
                if (completedCount > 0)
                {
                    await this.Utilities.UpdateJobAsync(job.Type, job.Id, j =>
                    {
                        j.CompletedTaskCount = Math.Min(j.CompletedTaskCount + completedCount, j.TaskCount);
                    }, token);
                }

                if (job.Type == JobType.Diagnostics)
                {
                    var diagTest = await this.jobsTable.RetrieveAsync<InternalDiagnosticsTest>(
                        this.Utilities.GetDiagPartitionKey(job.DiagnosticTest.Category),
                        job.DiagnosticTest.Name,
                        token);

                    if (diagTest?.TaskResultFilterScript?.Name != null)
                    {
                        var scriptBlob = this.Utilities.GetBlob(diagTest.TaskResultFilterScript.ContainerName, diagTest.TaskResultFilterScript.Name);
                        var path = Path.GetTempFileName();
                        await scriptBlob.DownloadToFileAsync(path, FileMode.Create, null, null, null, token);
                        var hookResults = await T.Task.WhenAll(tasks.Select(tid => this.TaskResultHook(job, tid.Id, path, token)));
                        if (hookResults.Any(r => !r)) return false;
                    }
                }

                if (job.FailJobOnTaskFailure && tasks.Any(t => t.ExitCode != 0))
                {
                    await this.Utilities.UpdateJobAsync(job.Type, job.Id, j =>
                    {
                        j.State = JobState.Failed;
                    }, token);

                    return true;
                }

                if (job.State == JobState.Running)
                {
                    var childIds = await T.Task.WhenAll(tasks.Select(async t => new { t.Id, ChildIds = t.ChildIds ?? await this.Utilities.LoadTaskChildIdsAsync(t.Id, job.Id, job.RequeueCount, token) }));

                    foreach (var cids in childIds)
                    {
                        this.Logger.Information("{0} Job {1} requeuecount {2}, task {3} completed, child ids {4}", job.Type, job.Id, job.RequeueCount, cids.Id, string.Join(",", cids.ChildIds));
                    }

                    var childIdGroups = childIds
                        .SelectMany(ids => ids.ChildIds.Select(cid => new { ParentId = ids.Id, ChildId = cid }))
                        .GroupBy(idPair => idPair.ChildId)
                        .Select(g => new { ChildId = g.Key, Count = g.Count(), ParentIds = g.Select(idPair => idPair.ParentId).ToList() }).ToList();

                    var childResults = await T.Task.WhenAll(childIdGroups.Select(async cid =>
                    {
                        this.Logger.Information("{0} Job {1} requeuecount {2}, task {3} has {4} ancestor tasks completed {5}", job.Type, job.Id, job.RequeueCount, cid.ChildId, cid.Count, string.Join(",", cid.ParentIds));
                        var childTaskKey = this.Utilities.GetTaskKey(job.Id, cid.ChildId, job.RequeueCount);

                        bool unlocked = false;
                        bool isEndTask = false;
                        Task childTask = null;
                        if (!await this.Utilities.UpdateTaskAsync(jobPartitionKey, childTaskKey, t =>
                        {
                            var parentIds = new HashSet<int>(Compress.UnZip(t.ZippedParentIds).Split(',').Select(_ => int.Parse(_)));
                            var oldParentIdsCount = parentIds.Count;
                            this.Logger.Information("{0} Job {1} requeuecount {2}, task {3} has {4} parent tasks {5}", job.Type, job.Id, job.RequeueCount, cid.ChildId, oldParentIdsCount, string.Join(",", parentIds));
                            cid.ParentIds.ForEach(_ => parentIds.Remove(_));
                            var newParentIdsStr = string.Join(",", parentIds);
                            this.Logger.Information("{0} Job {1} requeuecount {2}, after remove, task {3} has {4} parent tasks {5}", job.Type, job.Id, job.RequeueCount, cid.ChildId, parentIds.Count, newParentIdsStr);
                            if (parentIds.Count + cid.Count != oldParentIdsCount)
                            {
                                this.Logger.Warning("{0} Job {1} requeuecount {2}, task {3}, ids mismatch!", job.Type, job.Id, job.RequeueCount, cid.ChildId);
                            }

                            t.RemainingParentCount = parentIds.Count;
                            t.ZippedParentIds = Compress.GZip(newParentIdsStr);
                            unlocked = t.RemainingParentCount == 0;
                            isEndTask = t.Id == int.MaxValue;
                            if (unlocked)
                            {
                                t.State = isEndTask ? TaskState.Finished : TaskState.Dispatching;
                            }

                            childTask = t;
                        }, token))
                        {
                            await this.Utilities.UpdateJobAsync(job.Type, job.Id, j =>
                            {
                                j.State = JobState.Failed;
                                // TODO: make event separate.
                                (j.Events ?? (j.Events = new List<Event>())).Add(new Event()
                                {
                                    Content = $"Unable to update task record {cid.ChildId}",
                                    Source = EventSource.Job,
                                    Type = EventType.Alert
                                });
                            }, token);
                            return false;
                        }

                        if (unlocked)
                        {
                            if (isEndTask)
                            {
                                await this.Utilities.UpdateJobAsync(job.Type, job.Id, j => j.State = j.State == JobState.Running ? JobState.Finishing : j.State, token);
                                var jobEventQueue = this.Utilities.GetJobEventQueue();
                                await jobEventQueue.AddMessageAsync(
                                    // todo: event message generation.
                                    new CloudQueueMessage(JsonConvert.SerializeObject(new JobEventMessage() { Id = job.Id, EventVerb = "finish", Type = job.Type })),
                                    null, null, null, null,
                                    token);
                            }
                            else
                            {
                                var queue = this.Utilities.GetNodeDispatchQueue(childTask.Node);
                                await queue.AddMessageAsync(
                                    new CloudQueueMessage(JsonConvert.SerializeObject(new TaskEventMessage() { EventVerb = "start", Id = childTask.Id, JobId = childTask.JobId, JobType = childTask.JobType, RequeueCount = job.RequeueCount }, Formatting.Indented)),
                                    null, null, null, null, token);
                                this.Logger.Information("Dispatched job {0} task {1} to node {2}", childTask.JobId, childTask.Id, childTask.Node);
                            }
                        }

                        return true;
                    }));

                    return childResults.All(r => r);
                }

                return true;
            }));

            return results.All(r => r);
        }
    }
}
