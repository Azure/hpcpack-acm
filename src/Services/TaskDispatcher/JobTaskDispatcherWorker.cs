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
    using Microsoft.WindowsAzure.Storage;

    internal class JobTaskDispatcherWorker : TaskItemWorker, IWorker
    {
        private readonly TaskItemSourceOptions options;
        private readonly ConcurrentDictionary<string, InternalDiagnosticsTest> diagTests = new ConcurrentDictionary<string, InternalDiagnosticsTest>();
        private readonly ConcurrentDictionary<string, string> taskFilterScript = new ConcurrentDictionary<string, string>();
        private CloudTable jobsTable;
        private Dictionary<int, Task> tasksDict;

        public JobTaskDispatcherWorker(IOptions<TaskItemSourceOptions> options) : base(options.Value)
        {
            this.options = options.Value;
        }

        private Job job;
        private string jobPartitionKey;
        private T.Task RunningGuardTask;

        private async T.Task<bool> CheckJobRunningAsync(CancellationToken token)
        {
            try
            {
                this.job = await this.jobsTable.RetrieveAsync<Job>(this.jobPartitionKey, this.Utilities.JobEntryKey, token);
            }
            catch(StorageException ex)
            {
                if (ex.InnerException is OperationCanceledException)
                {
                    throw ex.InnerException;
                }

                throw;
            }

            return this.job.State == JobState.Running;
        }

        private async T.Task RunningGuard(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested && await this.CheckJobRunningAsync(token))
                {
                    this.Logger.Information("Guard running for job {0}", this.jobPartitionKey);
                    await T.Task.Delay(10000, token);
                }

                this.shouldExit = true;
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                this.Logger.Error(ex, "Error happened in RunningGuard of job {0}", this.jobPartitionKey);
            }

            this.Logger.Information("Exiting Running guard for job {0}", this.jobPartitionKey);
        }

        public async T.Task InitializeAsync(JobType type, int jobId, CancellationToken token)
        {
            this.jobsTable = await this.Utilities.GetOrCreateJobsTableAsync(token);
            this.Source = new QueueMultiTaskItemSource(
                await this.Utilities.GetOrCreateJobTaskCompletionQueueAsync(jobId, token),
                this.options);

            this.Logger.Information("Get tasks called for job {0}", jobId);
            this.jobPartitionKey = this.Utilities.GetJobPartitionKey(type, jobId);
            this.job = await this.jobsTable.RetrieveAsync<Job>(jobPartitionKey, this.Utilities.JobEntryKey, token);

            if (job?.State != JobState.Running)
            {
                this.Logger.Warning("Skip processing the task completion of {0}. Job state {1}.", jobPartitionKey, job?.State);
                this.shouldExit = true;
                return;
            }

            var partitionQuery = this.Utilities.GetPartitionQueryString(jobPartitionKey);

            var rowKeyRangeQuery = this.Utilities.GetRowKeyRangeString(
                this.Utilities.GetTaskKey(jobId, 0, job.RequeueCount),
                this.Utilities.GetTaskKey(jobId, int.MaxValue, job.RequeueCount),
                true,
                true);

            var q = TableQuery.CombineFilters(partitionQuery, TableOperators.And, rowKeyRangeQuery);
            var tasks = await this.jobsTable.QueryAsync<Task>(q, null, token);

            var allTasks = (await T.Task.WhenAll(tasks.Select(async t =>
            {
                var task = t.Item3;
                task.ChildIds = task.ChildIds ?? await this.Utilities.LoadTaskChildIdsAsync(task.Id, job.Id, job.RequeueCount, token);
                var unzippedParentIds = Compress.UnZip(task.ZippedParentIds);
                task.RemainingParentIds = new HashSet<int>(unzippedParentIds.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(_ => int.Parse(_)));
                return task;
            }))).ToList();

            this.Logger.Information("{0} Job {1} expanded parent ids for {2} tasks", job.Type, job.Id, allTasks.Count);

            this.tasksDict = allTasks.ToDictionary(t => t.Id, t => t);
            var finishedTasks = allTasks.Where(t => t.State == TaskState.Canceled || t.State == TaskState.Failed || t.State == TaskState.Finished).ToList();

            finishedTasks.ForEach(ft =>
            {
                foreach (var cid in ft.ChildIds)
                {
                    this.tasksDict[cid].RemainingParentIds.Remove(ft.Id);
                }
            });

            this.RunningGuardTask = this.RunningGuard(token);
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
            }).ToList();

            this.Logger.Information("Do work for job {0}, tasks finished: {1}", jobPartitionKey, string.Join(",", messages.Select(t => t.Id)));
            var skippedTasks = string.Join(",", messages.Where(msg => msg.RequeueCount != job.RequeueCount).Select(msg => $"{msg.Id}.{msg.RequeueCount}"));
            if (!string.IsNullOrEmpty(skippedTasks))
            {
                this.Logger.Warning("Skip processing the task completion, job requeueCount {0}, tasks {1}.", job.RequeueCount, skippedTasks);
            }

            var tasks = messages.Where(msg => msg.RequeueCount == job.RequeueCount).ToList();

            foreach (var t in tasks)
            {
                var tt = this.tasksDict[t.Id];
                this.Logger.Information("{0} Job {1} requeuecount {2}, task {3} on {4} completed, child ids {5}", job.Type, job.Id, job.RequeueCount, t.Id, tt.Node, string.Join(",", this.tasksDict[t.Id].ChildIds));
                tt.State = t.ExitCode == 0 ? TaskState.Finished : TaskState.Failed;
            }

            var completedCount = this.tasksDict.Count(t => t.Value.State == TaskState.Canceled || t.Value.State == TaskState.Finished || t.Value.State == TaskState.Failed);
            await this.Utilities.UpdateJobAsync(job.Type, job.Id, j =>
            {
                j.CompletedTaskCount = Math.Min(completedCount, j.TaskCount);
                this.job = j;
            }, token);

            if (job?.State != JobState.Running)
            {
                this.shouldExit = true;
                this.Logger.Warning("Skip processing the task completion of {0}. Job state {1}.", jobPartitionKey, job?.State);
                return true;
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
                        await T.Task.WhenAll(tasks.Select(tid => this.TaskResultHook(job, tid.Id, path, token)));
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

                this.job.State = JobState.Failed;

                return true;
            }

            var finishedTasks = tasks.Select(t => this.tasksDict[t.Id]);
            var childIdGroups = finishedTasks
                .SelectMany(ids => ids.ChildIds.Select(cid => new { ParentId = ids.Id, ChildId = cid }))
                .GroupBy(idPair => idPair.ChildId)
                .Select(g => new { ChildId = g.Key, Count = g.Count(), ParentIds = g.Select(idPair => idPair.ParentId).ToList() }).ToList();

            await T.Task.WhenAll(childIdGroups.Select(async cid =>
            {
                this.Logger.Information("{0} Job {1} requeuecount {2}, task {3} has {4} ancestor tasks completed {5}", job.Type, job.Id, job.RequeueCount, cid.ChildId, cid.Count, string.Join(",", cid.ParentIds));
                var toBeUnlocked = this.tasksDict[cid.ChildId];
                bool isEndTask = toBeUnlocked.Id == int.MaxValue;
                if (!isEndTask && (toBeUnlocked.State == TaskState.Canceled || toBeUnlocked.State == TaskState.Failed || toBeUnlocked.State == TaskState.Finished))
                {
                    this.Logger.Information("{0} Job {1} requeuecount {2}, task {3} is already completed", job.Type, job.Id, job.RequeueCount, cid.ChildId);
                    return;
                }

                var oldParentIdsCount = toBeUnlocked.RemainingParentIds.Count;
                var oldParents = string.Join(',', toBeUnlocked.RemainingParentIds);
                cid.ParentIds.ForEach(pid => toBeUnlocked.RemainingParentIds.Remove(pid));
                this.Logger.Information("Job {0}, requeueCount {1}, task {2} had {3} parents, remaining {4} parents, removed {5}",
                    job.Id, job.Request, cid.ChildId, oldParentIdsCount, toBeUnlocked.RemainingParentIds.Count, cid.ParentIds.Count);
                if (cid.ParentIds.Count + toBeUnlocked.RemainingParentIds.Count != oldParentIdsCount)
                {
                    this.Logger.Warning("Job {0}, requeueCount {1}, task {2} mismatch! old {3}, remaining {4}, removed {5}.",
                        job.Id, job.Request, cid.ChildId, oldParents, string.Join(',', toBeUnlocked.RemainingParentIds), string.Join(',', cid.ParentIds));
                }

                if (toBeUnlocked.RemainingParentIds.Count == 0)
                {
                    // unlocked
                    var childTaskKey = this.Utilities.GetTaskKey(job.Id, cid.ChildId, job.RequeueCount);
                    Task childTask = null;
                    if (!await this.Utilities.UpdateTaskAsync(jobPartitionKey, childTaskKey, t =>
                    {
                        t.ZippedParentIds = Compress.GZip("");
                        t.State = isEndTask ? TaskState.Finished : TaskState.Dispatching;
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
                    }

                    if (isEndTask)
                    {
                        await this.Utilities.UpdateJobAsync(job.Type, job.Id, j => j.State = j.State == JobState.Running ? JobState.Finishing : j.State, token);
                        var jobEventQueue = this.Utilities.GetJobEventQueue();
                        await jobEventQueue.AddMessageAsync(
                            // todo: event message generation.
                            new CloudQueueMessage(JsonConvert.SerializeObject(new JobEventMessage() { Id = job.Id, EventVerb = "finish", Type = job.Type })),
                            null, null, null, null,
                            token);

                        this.shouldExit = true;
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
            }));

            return true;
        }
    }
}
