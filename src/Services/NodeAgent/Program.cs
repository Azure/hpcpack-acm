namespace Microsoft.HpcAcm.Services.NodeAgent
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Microsoft.HpcAcm.Common.Utilities;
    using Microsoft.HpcAcm.Services.Common;

    public class Program
    {
        public static void Main(string[] args)
        {
            var taskMonitor = new TaskMonitor();

            using (var serverBuilder = BuildServer(args, taskMonitor))
            {
                serverBuilder.BuildAsync().GetAwaiter().GetResult();
                Task.Run(() => BuildWebHost(args, taskMonitor, serverBuilder.Utilities).Run());

                serverBuilder.Start();

                while (Console.In.Peek() == -1) { Task.Delay(1000).Wait(); }
                var logger = serverBuilder.LoggerFactory.CreateLogger<Program>();
                logger.LogInformation("Stop message received, stopping");

                serverBuilder.Stop();
            }
        }

        public static ServerBuilder BuildServer(string[] args, TaskMonitor monitor)
        {
            var nodeName = Environment.MachineName.ToLowerInvariant();

            var builder = new ServerBuilder();
            builder.ConfigureAppConfiguration(config =>
            {
                config.SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", false, true)
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
                await u.GetOrCreateNodeDispatchQueueAsync(nodeName, token),
                TimeSpan.FromSeconds(u.Option.VisibleTimeoutSeconds)))
            .AddWorker(async (config, u, l, token) => new NodeAgentWorker(
                config,
                l,
                await u.GetOrCreateJobsTableAsync(token),
                await u.GetOrCreateNodesTableAsync(token),
                u))
            .ConfigureWorker(w => ((NodeAgentWorker)w).Monitor = monitor);

            return builder;
        }

        public static IWebHost BuildWebHost(string[] args, TaskMonitor taskMonitor, CloudUtilities utilities) =>
            WebHost.CreateDefaultBuilder()
                .ConfigureAppConfiguration(c =>
                {
                    c.AddJsonFile("appsettings.json")
                        .AddEnvironmentVariables()
                        .AddCommandLine(args);
                })
                .ConfigureLogging((c, l) =>
                {
                    l.AddConfiguration(c.Configuration.GetSection("Logging"))
                        .AddConsole()
                        .AddDebug()
                        .AddAzureWebAppDiagnostics();
                })
                .ConfigureServices(services =>
                {
                    services.Add(new Extensions.DependencyInjection.ServiceDescriptor(typeof(TaskMonitor), taskMonitor));
                    services.Add(new Extensions.DependencyInjection.ServiceDescriptor(typeof(CloudUtilities), utilities));
                })
                .UseUrls("http://*:80", "http://*:5000")
                .UseStartup<Startup>()
                .Build();
    }
}
