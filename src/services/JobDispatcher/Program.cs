namespace Microsoft.HpcAcm.Services.JobDispatcher
{
    using System;
    using Microsoft.Extensions.Configuration;
    using Microsoft.HpcAcm.Services.Common;

    class Program
    {
        public static IConfigurationRoot Configuration;
        static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(@"E:\GitHub\hpc-acm\src\services\JobDispatcher")
                .AddJsonFile("appconfig.json", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables();

            Program.Configuration = builder.Build(); 

            var cloudOption = Configuration.GetSection("CloudOption").Get<CloudOption>();

            Console.WriteLine(cloudOption.StorageKeyOrSas);

            Console.WriteLine("Hello World!");
        }
    }
}
