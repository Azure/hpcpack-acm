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
    using Microsoft.Extensions.Options;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.HpcAcm.Common.Utilities;
    using Microsoft.HpcAcm.Services.Common;
    using Serilog;

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
                .ConfigureServices((context, services) =>
                {
                    var logOptions = context.Configuration.GetSection("LogOptions").Get<LogOptions>() ?? new LogOptions();
                    var loggerConfig = new LoggerConfiguration()
                        .WriteTo.Console()
                        .WriteTo.RollingFile(logOptions.FileName);

                    ILogger logger = loggerConfig.CreateLogger();

                    services.AddSingleton(logger);
                    var section = context.Configuration.GetSection("CloudOptions");
                    var options = section.Get<CloudOptions>();
                    var cloudUtilities = new CloudUtilities(Options.Create(options), logger);
                    cloudUtilities.InitializeAsync(CancellationToken.None).Wait();

                    services.Configure<CloudOptions>(section);
                    services.Configure<ServerOptions>(context.Configuration.GetSection("ServerOptions"));
                    services.AddSingleton(cloudUtilities);
                    services.AddSingleton<ServerObject>();
                    services.AddSingleton<DataProvider>();
                })
                .UseUrls("http://*:80", "http://*:5000")
                .UseStartup<Startup>()
                .Build();
    }
}
