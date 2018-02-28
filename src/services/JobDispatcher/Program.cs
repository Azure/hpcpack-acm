namespace Microsoft.HpcAcm.Services.JobDispatcher
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Microsoft.HpcAcm.Common.Dto;
    using Microsoft.HpcAcm.Common.Utilities;
    using Microsoft.HpcAcm.Services.Common;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Queue;
    using Microsoft.WindowsAzure.Storage.Table;

    class Program
    {
        static void Main(string[] args)
        {
            using (ServerBuilder builder = new ServerBuilder())
            {
                builder.ConfigureAppConfiguration(config =>
                {
                    config.SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile("appconfig.json", false, true)
                        .AddEnvironmentVariables();
                })
                .ConfigureLogging(l =>
                {
                    l.AddConsole().AddDebug();
                })
                .ConfigureCloudOptions(c => c.GetSection("CloudOption").Get<CloudOption>())
                .AddTaskItemSource(async (u, token) => new TaskItemSource(
                    await u.GetOrCreateJobDispatchQueueAsync(token),
                    TimeSpan.FromSeconds(u.Option.VisibleTimeoutSeconds)))
                .AddWorker(async (u, l, token) => new JobDispatcherWorker(
                    l,
                    await u.GetOrCreateDiagnosticsJobTableAsync(token),
                    u));

                builder.BuildAndStart();

                while (!Console.KeyAvailable) { Task.Delay(1000).Wait(); }
                var logger = builder.LoggerFactory.CreateLogger<Program>();
                logger.LogInformation("Stop message received, stopping");

                builder.Stop();
            }
        }
    }
}
