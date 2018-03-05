namespace Microsoft.HpcAcm.Services.JobRunner
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

    internal class JobRunnerWorker : WorkerBase
    {
        private ILogger logger;
        private CloudUtilities utilities;
        private CloudTable jobTable;

        private IDictionary<string, CloudQueue> nodesJobQueue = new Dictionary<string, CloudQueue>();

        public JobRunnerWorker(ILoggerFactory loggerFactory, CloudTable jobTable, CloudUtilities utilities)
        {
            this.logger = loggerFactory.CreateLogger<JobRunnerWorker>();
            this.utilities = utilities;
            this.jobTable = jobTable;
        }

        public override async Task DoWorkAsync(TaskItem taskItem, CancellationToken token)
        {
            var message = taskItem.GetMessage<JobDispatchMessage>();
            using (this.logger.BeginScope("Do work for JobDispatchMessage {0}", message.Id))
            {
                var result = await this.jobTable.ExecuteAsync(
                    TableOperation.Retrieve<DiagnosticsJobTableEntity>(
                        this.utilities.GetJobPartitionName(message.Id), 
                        this.utilities.JobEntryKey),
                    null,
                    null,
                    token);


                DiagnosticsJob job = null;
                if (result.Result is DiagnosticsJobTableEntity entity)
                {
                    job = entity.Job;

                    job.State = JobState.Running;

                    await Task.WhenAll(job.TargetNodes.Select(async n =>
                    {
                        var q = await this.utilities.GetOrCreateNodeDispatchQueueAsync(n, token);
                        await q.AddMessageAsync(new CloudQueueMessage(JsonConvert.SerializeObject(job)), null, null, null, null, token);
                    }));

                    result = await this.jobTable.ExecuteAsync(TableOperation.Merge(entity), null, null, token);

                    this.logger.LogInformation("Dispatched job, update job result code {0}", result.HttpStatusCode);
                }
            }                
        }
    }
}
