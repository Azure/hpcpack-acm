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
                        .AddEnvironmentVariables()
                        .AddCommandLine(args);
                })
                .ConfigureLogging((config, l) =>
                {
                    var debugLevel = config.GetValue<Extensions.Logging.LogLevel>("Logging:Debug:LogLevel:Default", Extensions.Logging.LogLevel.Debug);

                    l.AddConsole(config.GetSection("Logging").GetSection("Console"))
                        .AddDebug(debugLevel);
                })
                .ConfigureCloudOptions(c => c.GetSection("CloudOption").Get<CloudOption>())
                .AddTaskItemSource(async (u, token) => new TaskItemSource(
                    await u.GetOrCreateJobDispatchQueueAsync(token),
                    TimeSpan.FromSeconds(u.Option.VisibleTimeoutSeconds)))
                .AddWorker(async (u, l, token) => new JobDispatcherWorker(
                    l,
                    await u.GetOrCreateJobsTableAsync(token),
                    u));

                builder.BuildAndStart();

                while (Console.In.Peek() == -1) { Task.Delay(1000).Wait(); }
                var logger = builder.LoggerFactory.CreateLogger<Program>();
                logger.LogInformation("Stop message received, stopping");

                builder.Stop();
            }
        }
    }
}
