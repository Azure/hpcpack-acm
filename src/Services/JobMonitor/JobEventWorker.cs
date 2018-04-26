namespace Microsoft.HpcAcm.Services.JobMonitor
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Microsoft.HpcAcm.Services.Common;
    using Microsoft.HpcAcm.Common.Dto;
    using System.Threading;
    using System.Threading.Tasks;
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
        private readonly Dictionary<string, Dictionary<JobType, IJobEventProcessor>> processors;
        private readonly List<IJobEventProcessor> processorsList;

        public JobEventWorker(IOptions<JobEventWorkerOptions> options, IEnumerable<IJobEventProcessor> processors)
           : base(options.Value)
        {
            this.options = options.Value;

            this.processors = processors.GroupBy(p => p.EventVerb)
                .ToDictionary(g => g.Key, g => g.ToDictionary(e => e.RestrictedJobType, e => e));
            this.processorsList = processors.ToList();
        }

        private CloudTable jobsTable;

        public override async Task InitializeAsync(CancellationToken token)
        {
            this.jobsTable = await this.Utilities.GetOrCreateJobsTableAsync(token);

            this.Source = new QueueTaskItemSource(
                await this.Utilities.GetOrCreateJobEventQueueAsync(token),
                TimeSpan.FromSeconds(this.options.VisibleTimeoutSeconds),
                TimeSpan.FromSeconds(this.options.RetryIntervalSeconds));

            this.processorsList.OfType<ServerObject>().ToList().ForEach(so => so.CopyFrom(this));

            await base.InitializeAsync(token);
        }

        public override async Task<bool> ProcessTaskItemAsync(TaskItem taskItem, CancellationToken token)
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
                        if (this.processors.TryGetValue(message.EventVerb, out var verbProcessors) && verbProcessors.TryGetValue(message.Type, out var processor))
                        {
                            await processor.ProcessAsync(job, message, token);
                            this.Logger.LogInformation("Finished job {0} {1}", job.Id, job.State);
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
                        this.Logger.LogError("Exception occurred when process job {0}, {1}, {2}", job.Id, job.Type, message.EventVerb);
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
