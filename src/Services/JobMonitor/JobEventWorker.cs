namespace Microsoft.HpcAcm.Services.JobMonitor
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Microsoft.HpcAcm.Services.Common;
    using Microsoft.HpcAcm.Common.Dto;
    using System.Threading;
    using T = System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Microsoft.HpcAcm.Common.Utilities;
    using Microsoft.WindowsAzure.Storage.Table;
    using Microsoft.WindowsAzure.Storage.Queue;
    using Newtonsoft.Json;
    using System.Linq;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Options;
    using System.Diagnostics;

    internal class JobEventWorker : TaskItemWorker, IWorker
    {
        private readonly JobEventWorkerOptions options;
        private readonly Dictionary<string, Type> ActionHandlerTypes = new Dictionary<string, Type>()
        {
            { "cancel", typeof(JobCanceler) },
            { "progress", typeof(JobProgressHandler) },
            { "finish", typeof(JobFinisher) },
            { "dispatch", typeof(JobDispatcher) },
        };

        private readonly Dictionary<JobType, Type> JobTypeHandlers = new Dictionary<JobType, Type>()
        {
            { JobType.ClusRun, typeof(ClusrunJobHandler) },
            { JobType.Diagnostics, typeof(DiagnosticsJobHandler) },
        };

        public JobEventWorker(IOptions<JobEventWorkerOptions> options)
           : base(options.Value)
        {
            this.options = options.Value;
        }

        private CloudTable jobsTable;

        public override async T.Task InitializeAsync(CancellationToken token)
        {
            this.jobsTable = await this.Utilities.GetOrCreateJobsTableAsync(token);

            this.Source = new QueueTaskItemSource(
                await this.Utilities.GetOrCreateJobEventQueueAsync(token),
                this.options);

            await base.InitializeAsync(token);
        }

        public override async T.Task<bool> ProcessTaskItemAsync(TaskItem taskItem, CancellationToken token)
        {
            var message = taskItem.GetMessage<JobEventMessage>();
            using (this.Logger.BeginScope("Do work for JobEvent {0}, {1}, {2}", message.Id, message.Type, message.EventVerb))
            {
                var jobPartitionKey = this.Utilities.GetJobPartitionKey(message.Type, message.Id);
                var jobEntryKey = this.Utilities.JobEntryKey;
                var job = await this.jobsTable.RetrieveAsync<Job>(
                    jobPartitionKey,
                    jobEntryKey,
                    token);

                this.Logger.LogInformation("Queried job table entity for job id {0}", message.Id);

                if (job != null)
                {
                    try
                    {
                        IJobTypeHandler typeHandler;
                        IJobActionHandler actionHandler;
                        if (this.ActionHandlerTypes.TryGetValue(message.EventVerb, out Type actionType) 
                            && this.JobTypeHandlers.TryGetValue(message.Type, out Type jobType)
                            && (null != (actionHandler = (IJobActionHandler)this.Provider.GetService(actionType)))
                            && (null != (typeHandler = (IJobTypeHandler)this.Provider.GetService(jobType))))
                        {
                            ((ServerObject)actionHandler).CopyFrom(this);
                            ((ServerObject)typeHandler).CopyFrom(this);
                            actionHandler.JobTypeHandler = typeHandler;
                            await actionHandler.ProcessAsync(job, message, token);
                            this.Logger.LogInformation("Processed {0} job {1} {2}", job.Type, job.Id, job.State);
                        }
                        else
                        {
                            this.Logger.LogWarning("No processors found for job type {0}, {1}, {2}", job.Type, job.Id, message.EventVerb);

                            await this.Utilities.UpdateJobAsync(job.Type, job.Id, j =>
                            {
                                j.State = JobState.Failed;
                                (j.Events ?? (j.Events = new List<Event>())).Add(new Event()
                                {
                                    Content = $"No processors found for job type {j.Type}, event {message.EventVerb}",
                                    Source = EventSource.Job,
                                    Type = EventType.Alert,
                                });
                            }, token);
                        }
                    }
                    catch (Exception ex)
                    {
                        this.Logger.LogError("Exception occurred when process job {0}, {1}, {2}, {3}", job.Id, job.Type, message.EventVerb, ex);
                        await this.Utilities.UpdateJobAsync(job.Type, job.Id, j =>
                        {
                            j.State = JobState.Failed;
                            (j.Events ?? (j.Events = new List<Event>())).Add(new Event()
                            {
                                Content = $"Exception occurred when process job {job.Id} {job.Type} {message.EventVerb}. {ex}",
                                Source = EventSource.Job,
                                Type = EventType.Alert,
                            });
                        }, token);
                    }

                    return true;
                }
                else
                {
                    Debug.Assert(false);
                    this.Logger.LogWarning("The entity queried is not of job type, {0}", message.Id);
                    return false;
                }
            }
        }
    }
}
