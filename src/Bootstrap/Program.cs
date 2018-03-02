namespace Microsoft.HpcAcm.Bootstrap
{
    using Microsoft.Extensions.CommandLineUtils;
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.Diagnostics;

    class Program
    {
        static void Main(string[] args)
        {
            var serviceCollection = new Dictionary<string, string>()
            {
                { "frontend", "/app/Frontend/Frontend.dll" },
                { "jobdispatcher", "/app/JobDispatcher/JobDispatcher.dll" },
            };

            var possibleServices = string.Join("|", serviceCollection.Keys);

            var app = new CommandLineApplication(throwOnUnexpectedArg: false);

            CommandOption sasToken = app.Option(
                "-s | --sasstring <string>",
                "The connection string to azure storage account.",
                CommandOptionType.SingleValue);

            app.HelpOption("-? | -h | --help");

            foreach (var s in serviceCollection)
            {
                app.Command(s.Key, t =>
                {
                    t.OnExecute(() =>
                    {
                        var p = Process.Start(
                            "dotnet",
                            string.Join(" ", s.Value, sasToken.HasValue() ? sasToken.Value() : ""));
                        p.WaitForExit();
                        return p.ExitCode;
                    });
                });
            }

            app.OnExecute(() =>
            {
                app.ShowHelp();
                return 0;
            });

            Environment.ExitCode = app.Execute(args);
        }
    }
}
