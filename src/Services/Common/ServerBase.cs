namespace Microsoft.HpcAcm.Services.Common
{
    using Microsoft.Extensions.Logging;
    using Microsoft.HpcAcm.Common.Utilities;
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    public class Server
    {
        private WorkerBase worker;
        private TaskItemSource source;
        private ILogger logger;

        public Server(TaskItemSource source, WorkerBase worker, ILoggerFactory loggerFactory)
        {
            this.worker = worker;
            this.source = source;
            this.logger = loggerFactory.CreateLogger<Server>();
        }

        public async Task RunAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var t = await this.source.FetchTaskItemAsync(token);

                    token.ThrowIfCancellationRequested();

                    await this.worker.DoWorkAsync(t, token);

                    token.ThrowIfCancellationRequested();

                    await t.FinishAsync(token);
                }
                catch(Exception ex)
                {
                    logger.LogError(ex, "Exception occurred in {0}", nameof(RunAsync));
                    await Task.Delay(5000);
                }
            }
        }
    }
}
