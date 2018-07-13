namespace Microsoft.HpcAcm.Services.NodeAgent
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Microsoft.HpcAcm.Services.Common;
    using Microsoft.HpcAcm.Common.Dto;
    using Microsoft.HpcAcm.Common.Utilities;
    using System.Threading;
    using T = System.Threading.Tasks;
    using Serilog;
    using Microsoft.WindowsAzure.Storage.Table;
    using Microsoft.WindowsAzure.Storage.Queue;
    using Newtonsoft.Json;
    using System.Linq;
    using Microsoft.Extensions.Configuration;
    using System.Diagnostics;
    using Microsoft.Extensions.Options;
    using Microsoft.Extensions.DependencyInjection;

    internal class NodeAgentWorker : ServerObject, IWorker
    {
        private readonly NodeAgentWorkerOptions options;
        private List<IWorker> workers;

        public NodeAgentWorker(IOptions<NodeAgentWorkerOptions> options)
        {
            this.options = options.Value;
        }

        public async T.Task DoWorkAsync(CancellationToken token)
        {
            await T.Task.WhenAll(this.workers.Select(w => w.DoWorkAsync(token)));
        }

        public async T.Task InitializeAsync(CancellationToken token)
        {
            var dispatchWorkers = Enumerable.Range(0, this.options.DispatchWorkerCount).Select(i => (IWorker)this.Provider.GetRequiredService<JobDispatchWorker>());
            var cancelWorkers = Enumerable.Range(0, this.options.CancelWorkerCount).Select(i => (IWorker)this.Provider.GetRequiredService<JobCancelWorker>());

            this.workers = dispatchWorkers.Concat(cancelWorkers).ToList();
            this.workers.ForEach(w => ((ServerObject)w).CopyFrom(this));
            await T.Task.WhenAll(this.workers.Select(w => w.InitializeAsync(token)));
        }
    }
}
