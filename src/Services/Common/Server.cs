namespace Microsoft.HpcAcm.Services.Common
{
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Microsoft.HpcAcm.Common.Utilities;
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    public class Server
    {
        private readonly WorkerBase worker;
        private readonly TaskItemSource source;
        private readonly ILogger logger;
        private readonly ServerOptions options;

        public Server(TaskItemSource source, WorkerBase worker, ILoggerFactory loggerFactory, ServerOptions options)
        {
            this.worker = worker;
            this.source = source;
            this.options = options;
            this.logger = loggerFactory.CreateLogger<Server>();
        }

        public async Task RunAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var t = await this.source.FetchTaskItemAsync(token);
                    if (t == null)
                    {
                        this.logger.LogInformation("RunAsync, no tasks fetched. Sleep for {0} seconds", this.options.FetchIntervalSeconds);
                        await Task.Delay(TimeSpan.FromSeconds(this.options.FetchIntervalSeconds), token);
                        continue;
                    }

                    token.ThrowIfCancellationRequested();

                    bool result = await this.worker.DoWorkAsync(t, token);

                    token.ThrowIfCancellationRequested();

                    if (result)
                    {
                        await t.FinishAsync(token);
                    }
                    else
                    {
                        this.logger.LogInformation("RunAsync, failed to process the task.");
                    }
                }
                catch(Exception ex)
                {
                    logger.LogError(ex, "Exception occurred in {0}", nameof(RunAsync));
                    await Task.Delay(TimeSpan.FromSeconds(this.options.FetchIntervalOnErrorSeconds), token);
                }
            }
        }
    }
}
