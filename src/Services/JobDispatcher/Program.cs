namespace Microsoft.HpcAcm.Services.JobMonitor
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.HpcAcm.Common.Dto;
    using Microsoft.HpcAcm.Common.Utilities;
    using Microsoft.HpcAcm.Services.Common;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Queue;
    using Microsoft.WindowsAzure.Storage.Table;

    class Program
    {
        protected Program() { }

        static void Main(string[] args)
        {
            using (ServerBuilder builder = BuildServer(args))
            {
                var server = builder.BuildAsync().GetAwaiter().GetResult();
                server.Start(builder.CancelToken);

                while (Console.In.Peek() == -1) { Task.Delay(1000).Wait(); }
                var logger = builder.LoggerFactory.CreateLogger<Program>();
                logger.LogInformation("Stop message received, stopping");

                builder.Stop();
            }
        }

        static ServerBuilder BuildServer(string[] args) => new ServerBuilder(args)
            .ConfigServiceCollection((svc, config, token) =>
            {
                svc.Configure<JobEventWorkerOptions>(config.GetSection(nameof(JobEventWorkerOptions)));
                svc.AddSingleton<IJobEventProcessor, ClusrunJobDispatcher>();
                svc.AddSingleton<IJobEventProcessor, DiagnosticsJobDispatcher>();
                svc.AddSingleton<IJobEventProcessor, DiagnosticsJobFinisher>();
                svc.AddSingleton<IJobEventProcessor, ClusRunJobFinisher>();
                svc.AddSingleton<IWorker, JobEventWorker>();
            });
    }
}
