namespace Microsoft.HpcAcm.Frontend
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.HpcAcm.Common.Utilities;

    public class Program
    {
        protected Program() { }

        public static void Main(string[] args)
        {
            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
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
                .ConfigureServices((context, services) =>
                {
                    services.Configure<CloudOptions>(context.Configuration.GetSection("CloudOptions"));
                    services.AddSingleton<CloudUtilities>();
                })
                .UseUrls("http://*:80", "http://*:5000")
                .UseStartup<Startup>()
                .Build();
    }
}
