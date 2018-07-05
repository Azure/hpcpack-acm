namespace Microsoft.HpcAcm.Bootstrap
{
    using Microsoft.Extensions.CommandLineUtils;
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;

    class Program
    {
        static void Main(string[] args)
        {
            var serviceCollection = new Dictionary<string, string>()
            {
                { "frontend", "/app/Frontend/Frontend.dll" },
                { "jobmonitor", "/app/JobMonitor/JobMonitor.dll" },
                { "dashboard", "/app/Dashboard/Dashboard.dll" },
                { "taskdispatcher", "/app/TaskDispatcher/TaskDispatcher.dll" },
                { "nodeagent", "/app/NodeAgent/NodeAgent.dll" },
            };

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
                        var psi = new ProcessStartInfo("dotnet", string.Join(" ", s.Value, $"--sasToken={(sasToken.HasValue() ? sasToken.Value() : "")}"))
                        {
                            WorkingDirectory = Path.GetDirectoryName(s.Value)
                        };

                        var p = Process.Start(psi);
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
