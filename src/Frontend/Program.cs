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
    using Microsoft.Extensions.Logging;
    using Microsoft.HpcAcm.Common.Utilities;

    public class Program
    {
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
                    var option = context.Configuration.GetSection("CloudOption").Get<CloudOption>();
                    services.Add(new Extensions.DependencyInjection.ServiceDescriptor(typeof(CloudOption), option));
                    var utilities = new CloudUtilities(option);
                    utilities.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();
                    services.Add(new Extensions.DependencyInjection.ServiceDescriptor(typeof(CloudUtilities), utilities));
                })
                .UseUrls("http://*:80", "http://*:5000")
                .UseStartup<Startup>()
                .Build();
    }
}
