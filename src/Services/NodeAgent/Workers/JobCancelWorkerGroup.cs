namespace Microsoft.HpcAcm.Services.NodeAgent
{
    using Microsoft.Extensions.Options;
    using Microsoft.HpcAcm.Services.Common;
    using Microsoft.HpcAcm.Common.Utilities;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    internal class JobCancelWorkerGroup : WorkerGroup<JobCancelWorker>
    {
        public JobCancelWorkerGroup(IOptions<JobCancelWorkerGroupOptions> options) : base(options)
        {

        }

        public override async Task InitializeAsync(CancellationToken token)
        {
            await this.Utilities.GetOrCreateNodeCancelQueueAsync(this.ServerOptions.HostName, token);
            await base.InitializeAsync(token);
        }
    }
}
