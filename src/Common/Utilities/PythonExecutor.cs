namespace Microsoft.HpcAcm.Common.Utilities
{
    using Microsoft.WindowsAzure.Storage.Blob;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    public static class PythonExecutor
    {
        public static async Task<(int, string, string)> ExecuteAsync(CloudBlob blob, object stdin = null, CancellationToken token = default(CancellationToken))
        {
            var path = Path.GetTempFileName();
            try
            {
                await blob.DownloadToFileAsync(path, FileMode.Create, null, null, null, token);
                return await ExecuteAsync(path, stdin, token);
            }
            finally
            {
                File.Delete(path);
            }
        }

        public static async Task<(int, string, string)> ExecuteScriptAsync(string script, object stdin = null, CancellationToken token = default(CancellationToken))
        {
            var path = Path.GetTempFileName();
            try
            {
                File.WriteAllText(path, script);
                return await ExecuteAsync(path, stdin, token);
            }
            finally
            {
                File.Delete(path);
            }
        }

        public static async Task<(int, string, string)> ExecuteAsync(string path, object stdin = null, CancellationToken token = default(CancellationToken))
        {
            var psi = new ProcessStartInfo(@"python", path)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
            };

            // TODO: async wait process exit.
            using (var process = new Process() { StartInfo = psi, EnableRaisingEvents = true })
            {
                try
                {
                    StringBuilder output = new StringBuilder();
                    StringBuilder error = new StringBuilder();

                    process.OutputDataReceived += (s, e) => output.Append(e.Data);
                    process.ErrorDataReceived += (s, e) => error.Append(e.Data);

                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    if (stdin != null)
                    {
                        var jsonIn = JsonConvert.SerializeObject(stdin, Formatting.Indented);
                        await process.StandardInput.WriteAsync(jsonIn);
                        await process.StandardInput.FlushAsync();
                        process.StandardInput.Close();
                    }

                    int waitTimeMS = 30000;
                    if (process.WaitForExit(waitTimeMS))
                    {
                        process.WaitForExit();
                        var exitCode = process.ExitCode;
                        return (exitCode, output.ToString(), error.ToString());
                    }
                    else
                    {
                        return (-1, output.ToString(), $"Timed out after {waitTimeMS} ms, {error.ToString()}");
                    }
                }
                finally
                {
                    if (!process.HasExited)
                    {
                        process.Kill();
                    }

                    process.Close();
                }
            }
        }
    }
}
