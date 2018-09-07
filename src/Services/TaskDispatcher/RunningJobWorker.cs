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
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;
    using System.Diagnostics;
    using System.IO;
    using System.Collections.Concurrent;

    internal class RunningJobWorker : TaskItemWorker, IWorker
    {
        private readonly TaskItemSourceOptions options;
        private CloudTable jobsTable;

        public RunningJobWorker(IOptions<TaskItemSourceOptions> options) : base(options.Value)
        {
            this.options = options.Value;
        }

        public override async T.Task InitializeAsync(CancellationToken token)
        {
            this.jobsTable = await this.Utilities.GetOrCreateJobsTableAsync(token);
            this.Source = new QueueTaskItemSource(
                await this.Utilities.GetOrCreateRunningJobQueueAsync(token),
                this.options);

            await base.InitializeAsync(token);
        }

        public override async T.Task<bool> ProcessTaskItemAsync(TaskItem taskItem, CancellationToken token)
        {
            var runningJobMessage = taskItem.GetMessage<RunningJobMessage>();
            this.Logger.Information("Do work for job {0}, requeueCount {1}, message {2}", runningJobMessage.JobId, runningJobMessage.RequeueCount, taskItem.Id);

            var worker = this.Provider.GetRequiredService<JobTaskDispatcherWorker>();
            worker.CopyFrom(this);
            await worker.InitializeAsync(runningJobMessage.JobType, runningJobMessage.JobId, token);
            await worker.DoWorkAsync(token);
            this.Logger.Information("Finished running for job {0}, requeueCount {1}, message {2}", runningJobMessage.JobId, runningJobMessage.RequeueCount, taskItem.Id);

            return true;
        }
    }
}
