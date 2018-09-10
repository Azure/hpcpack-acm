namespace Microsoft.HpcAcm.Services.Common
{
    using Microsoft.Extensions.Configuration;
    using Microsoft.HpcAcm.Common.Utilities;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    public class Server : ServerObject, IWorker
    {
        public List<IWorker> Workers { get; }

        public Server(IEnumerable<IWorker> workers)
        {
            this.Workers = workers.ToList();
        }

        public async Task InitializeAsync(CancellationToken token)
        {
            foreach (var w in this.Workers)
            {
                if (w is ServerObject so)
                {
                    so.CopyFrom(this);
                }

                await w.InitializeAsync(token);
            }
        }

        public Task DoWorkAsync(CancellationToken token) => this.RunAsync(token);

        public void Run(CancellationToken token)
        {
            this.RunAsync(token).Wait();
        }

        public async Task RunAsync(CancellationToken token)
        {
            this.Logger.Information("Server is starting");

            if (this.Workers == null)
            {
                this.Logger.Error("No workers configured");
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
                 catch (OperationCanceledException)
                 {
                     if (token.IsCancellationRequested) return;
                     else throw;
                 }
                 catch (Exception ex)
                 {
                     Console.Error.WriteLine(ex);
                 }
             });
        }
    }
}
