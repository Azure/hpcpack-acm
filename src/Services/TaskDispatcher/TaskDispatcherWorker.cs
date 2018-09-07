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
    using System.Collections.Concurrent;

    internal class TaskDispatcherWorker : TaskItemWorker, IWorker
    {
        private readonly TaskItemSourceOptions options;
        private ConcurrentDictionary<string, InternalDiagnosticsTest> diagTests = new ConcurrentDictionary<string, InternalDiagnosticsTest>();
        private ConcurrentDictionary<string, string> taskFilterScript = new ConcurrentDictionary<string, string>();

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
            var taskInfo = await this.jobsTable.RetrieveAsync<ComputeClusterTaskInformation>(jobPartitionKey, taskResultKey, token);
            if (taskInfo == null)
            {
                this.Logger.Information("There is no task info recorded for job {0}, task {1}, requeue {2}, skip the filter.", job.Id, taskId, job.RequeueCount);
                return true;
            }

            var filteredResult = await PythonExecutor.ExecuteAsync(path, new { Job = job, Task = taskInfo }, token);
            this.Logger.Information("Task filter script execution for job {0}, task {1}, filteredResult exit code {2}, result length {3}", job.Id, taskId, filteredResult.ExitCode, filteredResult.Output?.Length);
            taskInfo.Message = filteredResult.IsError ? taskInfo.Message : filteredResult.Output;

            await this.jobsTable.InsertOrReplaceAsync(jobPartitionKey, taskResultKey, taskInfo, token);

            if (filteredResult.IsError)
            {
                this.Logger.Error("There is an error in task filter script. {0}", filteredResult.ErrorMessage);
                await this.Utilities.UpdateJobAsync(job.Type, job.Id, j =>
                {
                    (j.Events ?? (j.Events = new List<Event>())).Add(new Event()
                    {
                        Content = filteredResult.ErrorMessage,
                        Source = EventSource.Job,
                        Type = EventType.Alert,
                    });

                    j.State = JobState.Failed;
                }, token);

                return true;
            }

            return true;
        }

        public override async T.Task<bool> ProcessTaskItemAsync(TaskItem taskItem, CancellationToken token)
        {
            var taskItems = taskItem.GetMessage<TaskItem[]>();
            var messages = taskItems.Select(ti =>
            {
                var msg = ti.GetMessage<TaskCompletionMessage>();
                this.Logger.Information("Do work for job {0}, task {1}, message {2}", msg.JobId, msg.Id, ti.Id);
                return msg;
            });

            var jobGroups = messages.GroupBy(msg => this.Utilities.GetJobPartitionKey(msg.JobType, msg.JobId));

            var results = await T.Task.WhenAll(jobGroups.Select(async jg =>
            {
                var jobPartitionKey = jg.Key;
                this.Logger.Information("Do work for job {0}, tasks finished: {1}", jobPartitionKey, string.Join(",", jg.Select(t => t.Id)));
                var job = await this.jobsTable.RetrieveAsync<Job>(jobPartitionKey, this.Utilities.JobEntryKey, token);
                if (job == null || job.State != JobState.Running)
                {
                    this.Logger.Warning("Skip processing the task completion of {0}. Job state {1}.", jobPartitionKey, job?.State);
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
                    string diagKey = job.DiagnosticTest.Category + job.DiagnosticTest.Name;
                    if (!this.diagTests.TryGetValue(diagKey, out InternalDiagnosticsTest diagTest))
                    {
                        diagTest = await this.jobsTable.RetrieveAsync<InternalDiagnosticsTest>(
                            this.Utilities.GetDiagPartitionKey(job.DiagnosticTest.Category),
                            job.DiagnosticTest.Name,
                            token);
                        this.diagTests.TryAdd(diagKey, diagTest);
                    }

                    if (diagTest?.TaskResultFilterScript?.Name != null)
                    {
                        if (!this.taskFilterScript.TryGetValue(diagKey, out string script))
                        {
                            var scriptBlob = this.Utilities.GetBlob(diagTest.TaskResultFilterScript.ContainerName, diagTest.TaskResultFilterScript.Name);
                            using (var stream = new MemoryStream())
                            {
                                await scriptBlob.DownloadToStreamAsync(stream, null, null, null, token);
                                stream.Seek(0, SeekOrigin.Begin);
                                using (StreamReader sr = new StreamReader(stream, true))
                                {
                                    script = await sr.ReadToEndAsync();
                                }
                            }

                            this.taskFilterScript.TryAdd(diagKey, script);
                        }

                        var path = Path.GetTempFileName();
                        try
                        {
                            await File.WriteAllTextAsync(path, script, token);
                            var hookResults = await T.Task.WhenAll(tasks.Select(tid => this.TaskResultHook(job, tid.Id, path, token)));
                            if (hookResults.Any(r => !r)) return false;
                        }
                        finally
                        {
                            File.Delete(path);
                        }
                    }
                }

                if (job.FailJobOnTaskFailure && tasks.Any(t => t.ExitCode != 0))
                {
                    this.Logger.Information("Fail the job because some tasks failed {0}", job.Id);
                    await this.Utilities.UpdateJobAsync(job.Type, job.Id, j =>
                    {
                        j.State = JobState.Failed;
                        (j.Events ?? (j.Events = new List<Event>())).Add(new Event()
                        {
                            Content = $"Fail the job because some tasks failed",
                            Source = EventSource.Job,
                            Type = EventType.Alert
                        });
                    }, token);

                    return true;
                }

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
                        var unzippedParentIds = Compress.UnZip(t.ZippedParentIds);
                        this.Logger.Information("{0} Job {1} task {2}, ZippedParentIds {3}, unzipped {4}", job.Type, job.Id, cid.ChildId, t.ZippedParentIds, unzippedParentIds);
                        HashSet<int> parentIds;
                        try
                        {
                            parentIds = new HashSet<int>(unzippedParentIds.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(_ => int.Parse(_)));
                        }
                        catch (FormatException ex)
                        {
                            this.Logger.Error("Error happened {0}, input string {1}, len {2}", ex, unzippedParentIds, unzippedParentIds.Length);
                            throw;
                        }

                        var oldParentIdsCount = parentIds.Count;
                        this.Logger.Information("{0} Job {1} requeuecount {2}, task {3} has {4} parent tasks {5}", job.Type, job.Id, job.RequeueCount, cid.ChildId, oldParentIdsCount, string.Join(",", parentIds));
                        cid.ParentIds.ForEach(_ => parentIds.Remove(_));
                        var newParentIdsStr = string.Join(",", parentIds);
                        this.Logger.Information("{0} Job {1} requeuecount {2}, after remove, task {3} has {4} parent tasks {5}", job.Type, job.Id, job.RequeueCount, cid.ChildId, parentIds.Count, newParentIdsStr);
                        if (parentIds.Count + cid.Count != oldParentIdsCount)
                        {
                            this.Logger.Warning("{0} Job {1} requeuecount {2}, task {3}, ids mismatch!", job.Type, job.Id, job.RequeueCount, cid.ChildId);
                        }

                        t.ZippedParentIds = Compress.GZip(newParentIdsStr);
                        unlocked = parentIds.Count == 0;
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

                        return true;
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
            }));

            return results.All(r => r);
        }
    }
}
