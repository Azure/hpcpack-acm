namespace Microsoft.HpcAcm.Services.JobDispatcher
{
    using System;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Microsoft.HpcAcm.Services.Common;

    class Program
    {
        public static IConfigurationRoot Configuration;
        public static ILogger Logger;

        static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(@"E:\GitHub\hpc-acm\src\services\JobDispatcher")
                .AddJsonFile("appconfig.json", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables();

            Program.Configuration = builder.Build(); 

            Program.Logger = new LoggerFactory().AddConsole().AddDebug().CreateLogger<Program>();

            Logger.LogInformation("test");
            var cloudOption = Configuration.GetSection("CloudOption").Get<CloudOption>();

            Console.WriteLine(cloudOption.StorageKeyOrSas);

            Console.WriteLine("Hello World!");
        }
    }
}
