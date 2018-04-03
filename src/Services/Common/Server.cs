namespace Microsoft.HpcAcm.Services.Common
{
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Microsoft.HpcAcm.Common.Utilities;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    public class Server
    {
        public List<IWorker> Workers { get; }
        private readonly ILogger logger;

        public Server(ILogger<Server> logger, IEnumerable<IWorker> workers)
        {
            this.logger = logger;
            this.Workers = workers.ToList();
        }

        public void Run(CancellationToken token)
        {
            this.RunAsync(token).Wait();
        }

        public async Task RunAsync(CancellationToken token)
        {
            this.logger.LogInformation("Server is starting");

            if (this.Workers == null)
            {
                this.logger.LogError("No workers configured");
                return;
            }

            await Task.WhenAll(this.Workers.Select(w => w.DoWorkAsync(token)));
        }

        public void Start(CancellationToken token)
        {
            Task.Run(async () =>
             {
                 try
                 {
                     await this.RunAsync(token);
                 }
                 catch (Exception ex)
                 {
                     Console.Error.WriteLine(ex);
                 }
             });
        }
    }
}
