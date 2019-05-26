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
    using Microsoft.Extensions.DependencyInjection;
    using Serilog;
    using Microsoft.HpcAcm.Common.Utilities;
    using Microsoft.HpcAcm.Services.Common;

    public class Program
    {
        protected Program() { }

        public static void Main(string[] args)
        {
            var taskMonitor = new TaskMonitor();

            using (var serverBuilder = BuildServer(args, taskMonitor))
            {
                var server = serverBuilder.BuildAsync().GetAwaiter().GetResult();
                Console.CancelKeyPress += (s, e) => { serverBuilder.Stop(); };
                var webHost = BuildWebHost(
                    args,
                    taskMonitor,
                    serverBuilder.Utilities,
                    serverBuilder.GetRequiredService<NodeSynchronizer>(),
                    serverBuilder.GetRequiredService<ILogger>(),
                    serverBuilder.GetRequiredService<NodeRegister>());

                server.Start(serverBuilder.CancelToken);
                webHost.RunAsync(serverBuilder.CancelToken);
                while (Console.In.Peek() == -1) { Task.Delay(1000).Wait(); }
                serverBuilder.Logger?.Information("Stop message received, stopping");
                serverBuilder.Stop();
            }
        }

        public static ServerBuilder BuildServer(string[] args, TaskMonitor monitor) =>
            new ServerBuilder(args)
                .ConfigServiceCollection((svc, config, token) =>
                {
                    svc.AddSingleton(monitor);
                    svc.Configure<TaskItemSourceOptions>(config.GetSection(nameof(TaskItemSourceOptions)));
                    svc.Configure<NodeCommunicatorOptions>(config.GetSection(nameof(NodeCommunicatorOptions)));
                    svc.Configure<NodeRegisterWorkerOptions>(config.GetSection(nameof(NodeRegisterWorkerOptions)));
                    svc.AddSingleton<NodeCommunicator>();
                    svc.AddSingleton<NodeSynchronizer>();
                    svc.Configure<MetadataWorkerOptions>(config.GetSection(nameof(MetadataWorkerOptions)));
                    svc.AddSingleton<IWorker, MetadataWorker>();
                    svc.AddSingleton<NodeRegister>();
                    svc.AddSingleton<IWorker, NodeRegisterWorker>();
                    svc.Configure<MetricsWorkerOptions>(config.GetSection(nameof(MetricsWorkerOptions)));
                    svc.AddSingleton<IWorker, MetricsWorker>();
                    svc.Configure<JobDispatchWorkerGroupOptions>(config.GetSection(nameof(JobDispatchWorkerGroupOptions)));
                    svc.Configure<JobCancelWorkerGroupOptions>(config.GetSection(nameof(JobCancelWorkerGroupOptions)));
                    svc.AddSingleton<IWorker, JobDispatchWorkerGroup>();
                    svc.AddSingleton<IWorker, JobCancelWorkerGroup>();
                    svc.AddTransient<JobCancelWorker>();
                    svc.AddTransient<JobDispatchWorker>();
                    svc.AddTransient<StartJobAndTaskProcessor>();
                    svc.AddTransient<CancelJobOrTaskProcessor>();
                    //TODO: Add the followings only on HPC Pack Head Node
                    svc.AddSingleton<ManagementClient>();
                    svc.AddSingleton<IWorker, ManagementSyncWorker>();
                    svc.AddSingleton<IWorker, ManagementOperationWorker>();
                });

        public static IWebHost BuildWebHost(string[] args, params object[] sharedServices) =>
            WebHost.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration(c =>
                {
                    c.AddJsonFile("appsettings.json")
                        .AddEnvironmentVariables()
                        .AddCommandLine(args);
                })
                .ConfigureServices(services =>
                {
                    foreach (var svc in sharedServices)
                    {
                        services.AddSingleton(svc.GetType(), svc);
                        if (svc is ILogger logger) services.AddSingleton<ILogger>(logger);
                    }
                })
                .UseUrls("http://*:8080", "http://*:5000")
                .UseStartup<Startup>()
                .Build();
    }
}
