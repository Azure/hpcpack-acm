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
        private readonly string GZipedEmpty = Compress.GZip(string.Empty);
        private readonly TaskItemSourceOptions options;
        private readonly ConcurrentDictionary<string, InternalDiagnosticsTest> diagTests = new ConcurrentDictionary<string, InternalDiagnosticsTest>();
        private readonly ConcurrentDictionary<string, string> taskFilterScript = new ConcurrentDictionary<string, string>();
        private CloudTable jobsTable;
        private Dictionary<int, Task> tasksDict;
        private readonly ConcurrentDictionary<int, CloudQueueMessage> taskTimeoutMessages = new ConcurrentDictionary<int, CloudQueueMessage>();
        private readonly ConcurrentDictionary<int, CloudQueueMessage> taskNodeTimeoutMessages = new ConcurrentDictionary<int, CloudQueueMessage>();
        private DateTimeOffset lastProgressUpdateTime = DateTimeOffset.MinValue;
        private int batchId = 0;

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
            catch (StorageException ex) when (ex.IsCancellation())
            {
                throw ex.InnerException;
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
                }, token, this.Logger);

                return true;
            }

            return true;
        }

        public async T.Task UpdateJobProgress(CancellationToken token)
        {
            var completedCount = this.tasksDict.Count(t => t.Value.State == TaskState.Canceled || t.Value.State == TaskState.Finished || t.Value.State == TaskState.Failed);
            this.Logger.Information("Updating job {0} completed count to {1}", jobPartitionKey, completedCount);

            await this.Utilities.UpdateJobAsync(job.Type, job.Id, j =>
            {
                j.CompletedTaskCount = Math.Min(completedCount, j.TaskCount);
                this.job = j;
            }, token,
            this.Logger);
        }

        public override async T.Task<bool> ProcessTaskItemAsync(TaskItem taskItem, CancellationToken token)
        {
            this.batchId++;
            var taskItems = taskItem.GetMessage<TaskItem[]>();
            this.Logger.Information("Entering batch {0}, size {1}", this.batchId, taskItems.Length);
            var messages = taskItems.Select(ti =>
            {
                var msg = ti.GetMessage<TaskCompletionMessage>();
                this.Logger.Information("    Do work for job {0}, task {1}, message {2}", msg.JobId, msg.Id, ti.Id);
                return msg;
            }).ToList();

            this.Logger.Information("Do work for job {0}, tasks finished: {1}", jobPartitionKey, string.Join(",", messages.Select(t => t.Id)));
            var skippedTasks = string.Join(",", messages.Where(msg => msg.RequeueCount != job.RequeueCount).Select(msg => $"{msg.Id}.{msg.RequeueCount}"));
            if (!string.IsNullOrEmpty(skippedTasks))
            {
                this.Logger.Warning("Skip processing the task completion, job requeueCount {0}, tasks {1}.", job.RequeueCount, skippedTasks);
            }

            var tasks = messages.Where(msg => msg.RequeueCount == job.RequeueCount).ToList();

            this.Logger.Information("Deleting timeout guard {0}", jobPartitionKey);
            var jobTaskCompletionQueue = this.Utilities.GetJobTaskCompletionQueue(job.Id);
            await T.Task.WhenAll(tasks.Where(t => !t.Timeouted).Select(async t =>
            {
                if (t.Id == 0) return;

                async T.Task DeleteTimeoutAsync(ConcurrentDictionary<int, CloudQueueMessage> dict, int jobId, int id, CloudQueue queue)
                {
                    if (dict.TryRemove(id, out var msg))
                    {
                        try
                        {
                            await queue.DeleteMessageAsync(msg.Id, msg.PopReceipt, null, null, token);
                            this.Logger.Information("    Deleted {0} timeout message for job {1}, task {2}, message {3}", queue.Name, jobId, id, msg.Id);
                        }
                        catch (StorageException ex)
                        {
                            if (ex.IsNotFound()) this.Logger.Information("    Not found the {0} timeout message {1} for job {2}, task {3}", queue.Name, msg.Id, jobId, id);
                            else if (ex.IsCancellation()) return;
                            else this.Logger.Warning(ex, "    Unable to delete the {0} timeout message {1} for job {2}, task {3}", queue.Name, msg.Id, jobId, id);
                        }
                    }
                    else
                    {
                        this.Logger.Information("    Cannot find the node timeout message in memory for job {0}, task {1}", job.Id, t.Id);
                    }
                }

                if (!this.tasksDict.TryGetValue(t.Id, out Task tt))
                {
                    this.Logger.Information("    Cannot find task for job {0}, task {1}", job.Id, t.Id);
                    return;
                }

                var nodeCancelQueue = this.Utilities.GetNodeCancelQueue(tt.Node);
                await T.Task.WhenAll(DeleteTimeoutAsync(this.taskNodeTimeoutMessages, job.Id, t.Id, nodeCancelQueue),
                    DeleteTimeoutAsync(this.taskTimeoutMessages, job.Id, t.Id, jobTaskCompletionQueue));
            }));

            this.Logger.Information("Updating tasks state in memory {0}", jobPartitionKey);
            foreach (var t in tasks)
            {
                var tt = this.tasksDict[t.Id];
                this.Logger.Information("    {0} Job {1} requeuecount {2}, task {3} on {4} completed, timeout {5}, currentState {6}, child ids {7}", job.Type, job.Id, job.RequeueCount, t.Id, tt.Node, t.Timeouted, tt.State, string.Join(",", this.tasksDict[t.Id].ChildIds));

                bool alreadyFinished = tt.State == TaskState.Finished || tt.State == TaskState.Canceled || tt.State == TaskState.Failed;

                tt.State = alreadyFinished ? tt.State : (t.Timeouted ? TaskState.Canceled : (t.ExitCode == 0 ? TaskState.Finished : TaskState.Failed));
            }

            this.Logger.Information("Updating tasks state for timeouted tasks in storage {0}", jobPartitionKey);
            await T.Task.WhenAll(tasks.Where(t => t.Timeouted).Select(async t =>
            {
                var key = this.Utilities.GetTaskKey(job.Id, t.Id, job.RequeueCount);

                TaskState state = TaskState.Canceled;
                await this.Utilities.UpdateTaskAsync(this.jobPartitionKey, key, task =>
                {
                    state = task.State = task.State != TaskState.Canceled && task.State != TaskState.Failed && task.State != TaskState.Finished ? TaskState.Canceled : task.State;
                },
                token, this.Logger);

                this.Logger.Information("    Updated {0}, task {1} state to {2}", job.Id, t.Id, state);
            }));


            if (this.batchId % 10 == 0 || (DateTimeOffset.UtcNow - this.lastProgressUpdateTime) > TimeSpan.FromSeconds(10.0))
            {
                await this.UpdateJobProgress(token);
                this.lastProgressUpdateTime = DateTimeOffset.UtcNow;
            }

            if (job?.State != JobState.Running)
            {
                this.shouldExit = true;
                this.Logger.Warning("Skip processing the task completion of {0}. Job state {1}.", jobPartitionKey, job?.State);
                return true;
            }

            if (job.Type == JobType.Diagnostics)
            {
                this.Logger.Information("Processing task filters for job {0}", jobPartitionKey);
                string diagKey = job.DiagnosticTest.Category + job.DiagnosticTest.Name;
                if (!this.diagTests.TryGetValue(diagKey, out InternalDiagnosticsTest diagTest))
                {
                    diagTest = await this.jobsTable.RetrieveAsync<InternalDiagnosticsTest>(
                        this.Utilities.GetDiagPartitionKey(job.DiagnosticTest.Category),
                        job.DiagnosticTest.Name,
                        token);
                    this.diagTests.TryAdd(diagKey, diagTest);
                }

                if (diagTest?.TaskResultFilterScript?.Name != null && diagTest.RunTaskResultFilter)
                {
                    this.Logger.Information("Run task filters for job {0}", jobPartitionKey);
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

            this.Logger.Information("Check FailOnTaskFailure for job {0}", jobPartitionKey);
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

                    this.job = j;
                }, token, this.Logger);

                return true;
            }

            this.Logger.Information("Fetching finished tasks for job {0}", jobPartitionKey);
            var finishedTasks = tasks.Select(t => this.tasksDict[t.Id]);
            this.Logger.Information("Converting to child Ids view for job {0}", jobPartitionKey);
            var childIdGroups = finishedTasks
                .SelectMany(ids => ids.ChildIds.Select(cid => new { ParentId = ids.Id, ChildId = cid }))
                .GroupBy(idPair => idPair.ChildId)
                .Select(g => new { ChildId = g.Key, Count = g.Count(), ParentIds = g.Select(idPair => idPair.ParentId).ToList() }).ToList();

            this.Logger.Information("Converted to child Ids view for job {0}, children count {1}", jobPartitionKey, childIdGroups.Count);
            await T.Task.WhenAll(childIdGroups.Select(async cid =>
            {
                this.Logger.Information("    {0} Job {1} requeuecount {2}, task {3} has {4} ancestor tasks completed {5}", job.Type, job.Id, job.RequeueCount, cid.ChildId, cid.Count, string.Join(",", cid.ParentIds));
                var toBeUnlocked = this.tasksDict[cid.ChildId];
                bool isEndTask = toBeUnlocked.Id == int.MaxValue;
                if (!isEndTask && (toBeUnlocked.State != TaskState.Queued))
                {
                    this.Logger.Information("    {0} Job {1} requeuecount {2}, task {3} is in state {4}, skip dispatching.", job.Type, job.Id, job.RequeueCount, cid.ChildId, toBeUnlocked.State);
                    return;
                }

                var oldParentIdsCount = toBeUnlocked.RemainingParentIds.Count;
                var oldParents = string.Join(',', toBeUnlocked.RemainingParentIds);
                cid.ParentIds.ForEach(pid => toBeUnlocked.RemainingParentIds.Remove(pid));
                this.Logger.Information("    Job {0}, requeueCount {1}, task {2} had {3} parents, remaining {4} parents, removed {5}",
                    job.Id, job.Request, cid.ChildId, oldParentIdsCount, toBeUnlocked.RemainingParentIds.Count, cid.ParentIds.Count);
                if (cid.ParentIds.Count + toBeUnlocked.RemainingParentIds.Count != oldParentIdsCount)
                {
                    this.Logger.Warning("    Job {0}, requeueCount {1}, task {2} mismatch! old {3}, remaining {4}, removed {5}.",
                        job.Id, job.Request, cid.ChildId, oldParents, string.Join(',', toBeUnlocked.RemainingParentIds), string.Join(',', cid.ParentIds));
                }

                if (toBeUnlocked.RemainingParentIds.Count == 0)
                {
                    // unlocked
                    var targetState = isEndTask ? TaskState.Finished : TaskState.Dispatching;
                    var childTaskKey = this.Utilities.GetTaskKey(job.Id, cid.ChildId, job.RequeueCount);
                    Task childTask = toBeUnlocked;

                    if (isEndTask)
                    {
                        await this.UpdateJobProgress(token);
                        await this.Utilities.UpdateJobAsync(job.Type, job.Id, j => j.State = j.State == JobState.Running ? JobState.Finishing : j.State, token, this.Logger);
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
                        async T.Task dispatch()
                        {
                            var dispatchQueue = this.Utilities.GetNodeDispatchQueue(childTask.Node);
                            await dispatchQueue.AddMessageAsync(
                                new CloudQueueMessage(JsonConvert.SerializeObject(new TaskEventMessage() { EventVerb = "start", Id = childTask.Id, JobId = childTask.JobId, JobType = childTask.JobType, RequeueCount = job.RequeueCount }, Formatting.Indented)),
                                TimeSpan.FromSeconds(childTask.MaximumRuntimeSeconds), null, null, null, token);
                        };

                        async T.Task cancel()
                        {
                            if (!this.taskNodeTimeoutMessages.ContainsKey(childTask.Id))
                            {
                                var taskTimeoutMessage = new CloudQueueMessage(
                                    JsonConvert.SerializeObject(new TaskEventMessage() { EventVerb = "timeout", Id = childTask.Id, JobId = childTask.JobId, JobType = childTask.JobType, RequeueCount = job.RequeueCount }, Formatting.Indented));
                                var cancelQueue = this.Utilities.GetNodeCancelQueue(childTask.Node);
                                await cancelQueue.AddMessageAsync(
                                    taskTimeoutMessage,
                                    null, TimeSpan.FromSeconds(childTask.MaximumRuntimeSeconds), null, null, token);

                                this.taskNodeTimeoutMessages.TryAdd(childTask.Id, taskTimeoutMessage);
                            }
                            else
                            {
                                this.Logger.Warning("    Cannot add taskNodeTimeout for job {0} task {1}", job.Id, childTask.Id);
                            }
                        };

                        async T.Task complete()
                        {
                            if (!this.taskTimeoutMessages.ContainsKey(childTask.Id))
                            {
                                var taskTimeoutMessage = new CloudQueueMessage(
                                    JsonConvert.SerializeObject(new TaskCompletionMessage() { ChildIds = childTask.ChildIds, ExitCode = -1, Id = childTask.Id, JobId = childTask.JobId, JobType = childTask.JobType, RequeueCount = childTask.RequeueCount, Timeouted = true }, Formatting.Indented));

                                await jobTaskCompletionQueue.AddMessageAsync(
                                    taskTimeoutMessage,
                                    null, TimeSpan.FromSeconds(childTask.MaximumRuntimeSeconds), null, null, token);

                                this.taskTimeoutMessages.TryAdd(childTask.Id, taskTimeoutMessage);
                            }
                            else
                            {
                                this.Logger.Warning("    Cannot add taskTimeout for job {0} task {1}", job.Id, childTask.Id);
                            }
                        };

                        await T.Task.WhenAll(dispatch(), cancel(), complete());
                        this.Logger.Information("    Dispatched job {0} task {1} to node {2}", childTask.JobId, childTask.Id, childTask.Node);
                    }

                    toBeUnlocked.State = targetState;
                    this.Logger.Information("    Updated job {0} task {1} state to {2} in memory", childTask.JobId, childTask.Id, childTask.State);
                }
            }));

            this.Logger.Information("Finished to process the batch of tasks of job {0}", jobPartitionKey);

            return true;
        }
    }
}
