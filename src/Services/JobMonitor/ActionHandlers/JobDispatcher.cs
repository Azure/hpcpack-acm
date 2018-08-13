namespace Microsoft.HpcAcm.Services.JobMonitor
{
    using Microsoft.HpcAcm.Common.Dto;
    using Microsoft.HpcAcm.Common.Utilities;
    using Microsoft.HpcAcm.Services.Common;
    using Microsoft.WindowsAzure.Storage.Queue;
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

    class JobDispatcher : JobActionHandlerBase
    {
        private (bool, string) FillData(IEnumerable<InternalTask> tasks, Job job)
        {
            // TODO: check circle
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


        public override async T.Task ProcessAsync(Job job, JobEventMessage message, CancellationToken token)
        {
            var jobTable = this.Utilities.GetJobsTable();

            if (job.State != JobState.Queued) return;

            var tasks = await this.JobTypeHandler.GenerateTasksAsync(job, token);
            if (tasks == null) { return; }
            var allParentIds = new HashSet<int>(tasks.SelectMany(t => t.ParentIds ?? new List<int>()));
            var endingIds = tasks.Where(t => !allParentIds.Contains(t.Id)).Select(t => t.Id).ToList();

            var startTask = InternalTask.CreateFrom(job);
            startTask.Id = 0;
            startTask.CustomizedData = InternalTask.StartTaskMark;
            tasks.ForEach(t =>
            {
                if (t.ParentIds == null || t.ParentIds.Count == 0) t.ParentIds = new List<int>() { startTask.Id };
                t.ChildIds?.Clear();
            });

            var endTask = InternalTask.CreateFrom(job);
            endTask.Id = int.MaxValue;
            endTask.CustomizedData = InternalTask.EndTaskMark;
            endTask.ParentIds = endingIds;

            tasks.Add(startTask);
            tasks.Add(endTask);

            var (success, msg) = this.FillData(tasks, job);
            if (!success)
            {
                this.Logger.Error(msg);
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

                return;
            }

            const int MaxChildIds = 1000;

            var taskInstances = tasks.Select(it =>
            {
                string zipppedParentIds = Compress.GZip(string.Join(",", it.ParentIds ?? new List<int>()));

                var childIds = it.ChildIds;
                childIds = childIds ?? new List<int>();
                childIds = childIds.Count > MaxChildIds ? null : childIds;

                return new Task()
                {
                    ChildIds = childIds,
                    ZippedParentIds = zipppedParentIds,
                    CommandLine = it.CommandLine,
                    CustomizedData = it.CustomizedData,
                    Id = it.Id,
                    JobId = it.JobId,
                    JobType = it.JobType,
                    Node = it.Node,
                    RemainingParentCount = it.RemainingParentIds?.Count ?? 0,
                    RequeueCount = it.RequeueCount,
                    State = string.Equals(it.CustomizedData, Task.StartTaskMark, StringComparison.OrdinalIgnoreCase) ? TaskState.Finished : TaskState.Queued,
                };
            }).ToList();

            var childIdsContent = tasks
                .Where(it => (it.ChildIds?.Count ?? 0) > MaxChildIds)
                .Select(it => new
                {
                    it.Id,
                    it.JobId,
                    it.RequeueCount,
                    it.ChildIds,
                })
                .ToList();

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
            }).ToList();

            var jobPartitionKey = this.Utilities.GetJobPartitionKey(job.Type, job.Id);
            await jobTable.InsertOrReplaceBatchAsync(token, taskInstances.Select(t => new JsonTableEntity(
                jobPartitionKey,
                this.Utilities.GetTaskKey(job.Id, t.Id, job.RequeueCount),
                t)).ToArray());

            if (childIdsContent.Select(cid => cid.Id).Distinct().Count() != childIdsContent.Count())
            {
                await this.Utilities.UpdateJobAsync(job.Type, job.Id, j =>
                {
                    j.State = JobState.Failed;
                    (j.Events ?? (j.Events = new List<Event>())).Add(new Event()
                    {
                        Content = $"Duplicate task ids found.",
                        Source = EventSource.Job,
                        Type = EventType.Alert,
                    });
                }, token);

                return;
            }

            await T.Task.WhenAll(childIdsContent.Select(async childIds =>
            {
                var taskKey = this.Utilities.GetTaskKey(childIds.JobId, childIds.Id, childIds.RequeueCount);
                var childIdsBlob = await this.Utilities.CreateOrReplaceTaskChildrenBlobAsync(taskKey, token);

                var jsonContent = JsonConvert.SerializeObject(childIds.ChildIds);
                await childIdsBlob.UploadTextAsync(jsonContent, Encoding.UTF8, null, null, null, token);
            }));

            await jobTable.InsertOrReplaceBatchAsync(token, taskInfos.Select(t => new JsonTableEntity(
                jobPartitionKey,
                this.Utilities.GetTaskInfoKey(job.Id, t.Id, job.RequeueCount),
                t)).ToArray());

            JobState state = JobState.Queued;
            await this.Utilities.UpdateJobAsync(job.Type, job.Id, j =>
            {
                state = j.State = (j.State == JobState.Queued ? JobState.Running : j.State);
                j.TaskCount = taskInstances.Count() - 2;
            }, token);

            if (state == JobState.Running)
            {
                var taskCompletionQueue = await this.Utilities.GetOrCreateTaskCompletionQueueAsync(token);
                await taskCompletionQueue.AddMessageAsync(new CloudQueueMessage(
                    JsonConvert.SerializeObject(new TaskCompletionMessage() { JobId = job.Id, Id = 0, JobType = job.Type, RequeueCount = job.RequeueCount, ChildIds = startTask.ChildIds })),
                    null, null, null, null, token);
            }
        }
    }
}
