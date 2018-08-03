namespace Microsoft.HpcAcm.Services.Common
{
    using Microsoft.Extensions.Options;
    using Microsoft.Extensions.DependencyInjection;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using T = System.Threading.Tasks;

    public class WorkerGroup<WorkerT> : ServerObject, IWorker where WorkerT : IWorker
    {
        private readonly WorkerGroupOptions options;
        private List<IWorker> workers;

        public WorkerGroup(IOptions<WorkerGroupOptions> options)
        {
            this.options = options.Value;
        }

        public async T.Task DoWorkAsync(CancellationToken token)
        {
            await T.Task.WhenAll(this.workers.Select(w => w.DoWorkAsync(token)));
        }

        public virtual async T.Task InitializeAsync(CancellationToken token)
        {
            this.workers = Enumerable.Range(0, this.options.WorkerCount).Select(i => (IWorker)this.Provider.GetRequiredService<WorkerT>()).ToList();

            this.workers.ForEach(w => ((ServerObject)w).CopyFrom(this));
            await T.Task.WhenAll(this.workers.Select(w => w.InitializeAsync(token)));
        }
    }
}
