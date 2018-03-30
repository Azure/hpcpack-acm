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

    internal class JobDispatcherWorker : WorkerBase
    {
        private readonly ILogger logger;
        private readonly CloudUtilities utilities;
        private readonly CloudTable jobTable;

        public JobDispatcherWorker(IConfiguration config, ILoggerFactory loggerFactory, CloudTable jobTable, CloudUtilities utilities)
        {
            this.Configuration = config;
            this.logger = loggerFactory.CreateLogger<JobDispatcherWorker>();
            this.utilities = utilities;
            this.jobTable = jobTable;
        }
        public IConfiguration Configuration { get; }

        public override async Task<bool> DoWorkAsync(TaskItem taskItem, CancellationToken token)
        {
            var message = taskItem.GetMessage<JobDispatchMessage>();
            using (this.logger.BeginScope("Do work for JobDispatchMessage {0}", message.Id))
            {
                var result = await this.jobTable.ExecuteAsync(
                    TableOperation.Retrieve<JsonTableEntity>(
                        this.utilities.GetJobPartitionKey($"{message.Type}", message.Id),
                        this.utilities.JobEntryKey),
                    null,
                    null,
                    token);

                this.logger.LogInformation("Queried job table entity for job id {0}, result {1}", message.Id, result.HttpStatusCode);

                if (result.Result is JsonTableEntity entity)
                {
                    var job = entity.GetObject<Job>();

                    job.State = JobState.Running;
                    var internalJob = InternalJob.CreateFrom(job);

                    await Task.WhenAll(internalJob.TargetNodes.Select(async n =>
                    {
                        var q = await this.utilities.GetOrCreateNodeDispatchQueueAsync(n, token);
                        await q.AddMessageAsync(new CloudQueueMessage(JsonConvert.SerializeObject(internalJob)), null, null, null, null, token);
                    }));

                    entity.PutObject(job);
                    result = await this.jobTable.ExecuteAsync(TableOperation.Replace(entity), null, null, token);

                    this.logger.LogInformation("Dispatched job, update job result code {0}", result.HttpStatusCode);
                    return result.IsSuccessfulStatusCode();
                }
                else
                {
                    this.logger.LogWarning("The entity queried is not of <JobTableEntity> type, {0}", result.Result);
                    return false;
                }
            }
        }
    }
}
