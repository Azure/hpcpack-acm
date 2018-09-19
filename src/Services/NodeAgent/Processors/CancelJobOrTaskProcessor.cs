namespace Microsoft.HpcAcm.Services.NodeAgent
{
    using Serilog;
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

        public override async T.Task<bool> ProcessAsync(TaskEventMessage message, DateTimeOffset? insertionTime, CancellationToken token)
        {
            var jobsTable = this.Utilities.GetJobsTable();
            var nodeName = this.ServerOptions.HostName;

            string taskKey = null;
            if (message.Id > 0) taskKey = this.Utilities.GetTaskKey(message.JobId, message.Id, message.RequeueCount);


            // TODO: cancel single task
            //if (message.Id != 0
            //{
            //    var taskKey = this.Utilities.GetTaskKey(message.JobId, message.Id, message.RequeueCount);
            //    var task = await this.jobsTable.RetrieveAsync<Task>(this.jobPartitionKey, taskKey, token);
            //    var taskResultKey = this.Utilities.GetTaskResultKey(task.JobId, task.Id, task.RequeueCount);
            //    var nodeTaskResultKey = this.Utilities.GetNodeTaskResultKey(nodeName, task.JobId, task.RequeueCount, task.Id);
            //}

            this.Logger.Information("Do work {0} for job {1} task {2} on node {3}", message.EventVerb, message.JobId, message.Id, nodeName);

            var jobPartitionKey = this.Utilities.GetJobPartitionKey(message.JobType, message.JobId);
            var job = await jobsTable.RetrieveAsync<Job>(jobPartitionKey, this.Utilities.JobEntryKey, token);

            if (job != null && job.RequeueCount != message.RequeueCount)
            {
                return true;
            }

            try
            {

                if (taskKey != null)
                {
                    await this.Utilities.UpdateTaskAsync(jobPartitionKey, taskKey, t => t.State = t.State == TaskState.Failed || t.State == TaskState.Finished ? t.State : TaskState.Canceled, token, this.Logger);
                    await this.Communicator.EndTaskAsync(nodeName, new EndTaskArg(null, message.JobId, message.Id), token);
                    this.Monitor.CancelTask(message.JobId, taskKey);
                }
                else
                {
                    // end the whole job
                    await this.Utilities.UpdateJobAsync(message.JobType, message.JobId, j => j.State = j.State == JobState.Failed || j.State == JobState.Finished ? j.State : JobState.Canceled, token, this.Logger);
                    await this.Communicator.EndJobAsync(nodeName, new EndJobArg(null, message.JobId), token);
                    this.Monitor.CancelJob(message.JobId);
                }
            }
            catch (Exception ex)
            {
                if (job != null)
                {
                    await this.Utilities.UpdateJobAsync(job.Type, job.Id, j =>
                    {
                        (j.Events ?? (j.Events = new List<Event>())).Add(new Event()
                        {
                            Type = EventType.Warning,
                            Source = EventSource.Job,
                            Content = $"Failed to end Job {job.Id}, exception {ex}",
                        });
                    }, token, this.Logger);
                }
            }

            return true;
        }
    }
}
