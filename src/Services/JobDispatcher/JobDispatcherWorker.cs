namespace Microsoft.HpcAcm.Services.JobDispatcher
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

    internal class JobDispatcherWorker : TaskItemWorker, IWorker
    {
        private readonly JobDispatcherOptions options;
        private readonly Dictionary<JobType, IDispatcher> dispatchers;

        public JobDispatcherWorker(IOptions<JobDispatcherOptions> options, IEnumerable<IDispatcher> dispatchers) : base(options.Value)
        {
            this.options = options.Value;
            this.dispatchers = dispatchers.ToDictionary(d => d.RestrictedJobType);
        }

        private CloudTable jobsTable;

        public override async Task InitializeAsync(CancellationToken token)
        {
            this.jobsTable = await this.Utilities.GetOrCreateJobsTableAsync(token);

            this.Source = new QueueTaskItemSource(
                await this.Utilities.GetOrCreateJobDispatchQueueAsync(token),
                TimeSpan.FromSeconds(this.options.VisibleTimeoutSeconds),
                TimeSpan.FromSeconds(this.options.RetryIntervalSeconds));

            this.dispatchers.Values.OfType<ServerObject>().ToList().ForEach(so => so.CopyFrom(this));

            await base.InitializeAsync(token);
        }

        public override async Task<bool> ProcessTaskItemAsync(TaskItem taskItem, CancellationToken token)
        {
            var message = taskItem.GetMessage<JobDispatchMessage>();
            using (this.Logger.BeginScope("Do work for JobDispatchMessage {0}", message.Id))
            {
                var result = await this.jobsTable.ExecuteAsync(
                    TableOperation.Retrieve<JsonTableEntity>(
                        this.Utilities.GetJobPartitionKey($"{message.Type}", message.Id),
                        this.Utilities.JobEntryKey),
                    null,
                    null,
                    token);

                this.Logger.LogInformation("Queried job table entity for job id {0}, result {1}", message.Id, result.HttpStatusCode);

                if (result.Result is JsonTableEntity entity)
                {
                    var job = entity.GetObject<Job>();

                    try
                    {
                        if (this.dispatchers.TryGetValue(job.Type, out var dispatcher))
                        {
                            await dispatcher.DispatchAsync(job, token);
                            job.State = JobState.Running;
                            entity.PutObject(job);
                            this.Logger.LogInformation("Dispatched job {0}", job.Id);
                        }
                        else
                        {
                            this.Logger.LogWarning("No dispatchers found for job type {0}, {1}, {2}", job.Type, job.Id, job.Name);
                            job.State = JobState.Failed;
                            (job.Events ?? (job.Events = new List<Event>())).Add(new Event() { Content = $"No dispatchers found for job type {job.Type}", Source = EventSource.Job, Type = EventType.Alert, Time = DateTimeOffset.UtcNow });
                        }
                    }
                    catch (Exception ex)
                    {
                        this.Logger.LogError("Exception occurred when dispatch job {0}, {1}", job.Id, job.Name);
                        job.State = JobState.Failed;
                        (job.Events ?? (job.Events = new List<Event>())).Add(new Event() { Content = $"Exception occurred when dispatch job {job.Id} {job.Name}. {ex}", Source = EventSource.Job, Type = EventType.Alert, Time = DateTimeOffset.UtcNow });
                    }

                    result = await this.jobsTable.ExecuteAsync(TableOperation.Replace(entity), null, null, token);
                    this.Logger.LogInformation("Update job {0} result code {1}", job.Id, result.HttpStatusCode);

                    return result.IsSuccessfulStatusCode();
                }
                else
                {
                    this.Logger.LogWarning("The entity queried is not of <JobTableEntity> type, {0}", result.Result);
                    return false;
                }
            }
        }
    }
}
