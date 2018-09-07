namespace Microsoft.HpcAcm.Services.JobMonitor
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Microsoft.HpcAcm.Services.Common;
    using Microsoft.HpcAcm.Common.Dto;
    using System.Threading;
    using T = System.Threading.Tasks;
    using Microsoft.HpcAcm.Common.Utilities;
    using Microsoft.WindowsAzure.Storage.Table;
    using Microsoft.WindowsAzure.Storage.Queue;
    using Newtonsoft.Json;
    using System.Linq;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Options;
    using System.Diagnostics;

    internal class JobEventWorkerGroup : WorkerGroup<JobEventWorker>
    {
        public JobEventWorkerGroup(IOptions<JobEventWorkerGroupOptions> options)
           : base(options)
        {
        }

        public override async T.Task InitializeAsync(CancellationToken token)
        {
            await this.Utilities.GetOrCreateJobsTableAsync(token);
            await this.Utilities.GetOrCreateRunningJobQueueAsync(token);
            await this.Utilities.GetOrCreateJobEventQueueAsync(token);
            await base.InitializeAsync(token);
        }
    }
}
