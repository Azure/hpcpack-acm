namespace Microsoft.HpcAcm.Services.JobMonitor
{
    using Microsoft.Extensions.Logging;
    using Microsoft.HpcAcm.Common.Dto;
    using Microsoft.HpcAcm.Common.Utilities;
    using Microsoft.HpcAcm.Services.Common;
    using Microsoft.WindowsAzure.Storage.Queue;
    using Microsoft.WindowsAzure.Storage.Table;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using T = System.Threading.Tasks;

    public abstract class JobDispatcher : ServerObject, IJobEventProcessor
    {
        public abstract JobType RestrictedJobType { get; }
        public string EventVerb { get => "dispatch"; }

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


        public async T.Task ProcessAsync(Job job, JobEventMessage message, CancellationToken token)
        {
            Debug.Assert(job.Type == this.RestrictedJobType, "Job type mismatch");

            var jobTable = this.Utilities.GetJobsTable();

            if (job.State != JobState.Queued) return;

            var tasks = await this.GenerateTasksAsync(job, token);
            if (tasks == null) { return; }
            var ids = tasks.Select(t => t.Id).ToList();

            var startTask = InternalTask.CreateFrom(job);
            startTask.Id = 0;
            startTask.CustomizedData = InternalTask.StartTaskMark;
            tasks.ForEach(t =>
            {
                (t.ParentIds ?? (t.ParentIds = new List<int>())).Add(startTask.Id);
                t.ChildIds?.Clear();
            });

            var endTask = InternalTask.CreateFrom(job);
            endTask.Id = int.MaxValue;
            endTask.CustomizedData = InternalTask.EndTaskMark;
            endTask.ParentIds = ids;

            tasks.Add(startTask);
            tasks.Add(endTask);

            var (success, msg) = this.FillData(tasks, job);
            if (!success)
            {
                this.Logger.LogError(msg);
                await this.Utilities.UpdateJobAsync(job.Type, job.Id, j =>
                {
                    j.State = JobState.Failed;
                    (j.Events ?? (j.Events = new List<Event>())).Add(new Event()
                    {
                        Content = msg,
                        Source = EventSource.Job,
                        Type = EventType.Alert
                    });
                }, token);
            }

            var taskInstances = tasks.Select(it => new Task()
            {
                ChildIds = it.ChildIds,
                CommandLine = it.CommandLine,
                CustomizedData = it.CustomizedData,
                Id = it.Id,
                JobId = it.JobId,
                JobType = it.JobType,
                Node = it.Node,
                ParentIds = it.ParentIds,
                RemainingParentIds = it.RemainingParentIds,
                RequeueCount = it.RequeueCount,
                State = string.Equals(it.CustomizedData, Task.StartTaskMark, StringComparison.OrdinalIgnoreCase) ? TaskState.Finished : TaskState.Queued,
            });

            var taskInfos = tasks.Select(it => new TaskStartInfo()
            {
                Id = it.Id,
                JobId = it.JobId,
                JobType = it.JobType,
                NodeName = it.Node,
                Password = it.Password,
                PrivateKey = it.PrivateKey,
                PublicKey = it.PublicKey,
                UserName = it.UserName,
                StartInfo = new HpcAcm.Common.Dto.ProcessStartInfo(it.CommandLine, it.WorkingDirectory, null, null, null, it.EnvironmentVariables, null, it.RequeueCount),
            });

            // TODO: batch size.
            var batch = new TableBatchOperation();
            var jobPartitionKey = this.Utilities.GetJobPartitionKey(job.Type, job.Id);
            foreach (var e in taskInstances.Select(t => new JsonTableEntity(
                jobPartitionKey,
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

            batch.Clear();
            foreach (var e in taskInfos.Select(t => new JsonTableEntity(
                jobPartitionKey,
                 this.Utilities.GetTaskInfoKey(job.Id, t.Id, job.RequeueCount),
                 t)))
            {
                batch.InsertOrReplace(e);
            }

            tableResults = await jobTable.ExecuteBatchAsync(batch, null, null, token);

            if (!tableResults.All(r => r.IsSuccessfulStatusCode()))
            {
                throw new InvalidOperationException("Not all tasks dispatched successfully");
            }

            JobState state = JobState.Queued;
            await this.Utilities.UpdateJobAsync(job.Type, job.Id, j => state = j.State = (j.State == JobState.Queued ? JobState.Running : j.State), token);

            if (state == JobState.Running)
            {
                var taskCompletionQueue = await this.Utilities.GetOrCreateTaskCompletionQueueAsync(token);
                await taskCompletionQueue.AddMessageAsync(new CloudQueueMessage(
                    JsonConvert.SerializeObject(new TaskCompletionMessage() { JobId = job.Id, Id = 0, JobType = job.Type, RequeueCount = job.RequeueCount })),
                    null, null, null, null, token);
            }
        }

        public abstract T.Task<List<InternalTask>> GenerateTasksAsync(Job job, CancellationToken token);
    }
}
