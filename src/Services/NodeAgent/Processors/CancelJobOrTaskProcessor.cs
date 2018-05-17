namespace Microsoft.HpcAcm.Services.NodeAgent
{
    using Microsoft.Extensions.Logging;
    using Microsoft.HpcAcm.Services.Common;
    using Microsoft.HpcAcm.Common.Utilities;
    using Microsoft.HpcAcm.Common.Dto;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using T = System.Threading.Tasks;
    using System.Text;
    using Microsoft.WindowsAzure.Storage.Queue;
    using Newtonsoft.Json;
    using Microsoft.WindowsAzure.Storage.Table;

    public class CancelJobOrTaskProcessor : JobTaskProcessor
    {
        private TaskMonitor Monitor { get; }

        public CancelJobOrTaskProcessor(TaskMonitor monitor, NodeCommunicator communicator) : base(communicator)
        {
            this.Monitor = monitor;
        }

        public override async T.Task<bool> ProcessAsync(TaskEventMessage message, CancellationToken token)
        {
            var jobsTable = this.Utilities.GetJobsTable();
            var nodeName = this.ServerOptions.HostName;

            var jobPartitionKey = this.Utilities.GetJobPartitionKey(message.JobType, message.JobId);
            var job = await jobsTable.RetrieveAsync<Job>(jobPartitionKey, this.Utilities.JobEntryKey, token);
            var taskKey = this.Utilities.GetTaskKey(message.JobId, message.Id, message.RequeueCount);

            if (job == null) return true;

            // TODO: cancel single task
            //if (message.Id != 0
            //{
            //    var taskKey = this.Utilities.GetTaskKey(message.JobId, message.Id, message.RequeueCount);
            //    var task = await this.jobsTable.RetrieveAsync<Task>(this.jobPartitionKey, taskKey, token);
            //    var taskResultKey = this.Utilities.GetTaskResultKey(task.JobId, task.Id, task.RequeueCount);
            //    var nodeTaskResultKey = this.Utilities.GetNodeTaskResultKey(nodeName, task.JobId, task.RequeueCount, task.Id);
            //}

            using (this.Logger.BeginScope("Do work {0} for job {1} on node {2}", message.EventVerb, job.Id, nodeName))
            {
                if (job.RequeueCount != message.RequeueCount)
                {
                    return true;
                }

                try
                {
                    if (job.State != JobState.Canceling && job.State != JobState.Canceled)
                    {
                        await this.Utilities.UpdateJobAsync(job.Type, job.Id, j =>
                        {
                            (j.Events ?? (j.Events = new List<Event>())).Add(new Event()
                            {
                                Type = EventType.Warning,
                                Source = EventSource.Job,
                                Content = $"Attempted to end the job {job.Id} when the job is in state {job.State}",
                            });
                        }, token);
                    }

                    await this.Utilities.UpdateTaskAsync(jobPartitionKey, taskKey, t => t.State = TaskState.Canceled, token);
                    await this.Communicator.EndJobAsync(nodeName, new EndJobArg(null, message.JobId), token);
                    this.Monitor.CancelTask(taskKey);
                }
                catch (Exception ex)
                {
                    await this.Utilities.UpdateJobAsync(job.Type, job.Id, j =>
                    {
                        (j.Events ?? (j.Events = new List<Event>())).Add(new Event()
                        {
                            Type = EventType.Warning,
                            Source = EventSource.Job,
                            Content = $"Failed to end Job {job.Id}, exception {ex}",
                        });
                    }, token);
                }

                return true;
            }
        }
    }
}
