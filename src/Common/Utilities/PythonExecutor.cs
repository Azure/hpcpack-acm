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
        public static async Task<(string, string)> ExecuteAsync(string path, CloudBlob blob, object stdin = null, CancellationToken token = default(CancellationToken))
        {
            await blob.DownloadToFileAsync(path, FileMode.Create, null, null, null, token);
            return await ExecuteAsync(path, stdin, token);
        }

        public static async Task<(string, string)> ExecuteAsync(string path, string script, object stdin = null, CancellationToken token = default(CancellationToken))
        {
            File.WriteAllText(path, script);
            return await ExecuteAsync(path, stdin, token);
        }

        public static async Task<(string, string)> ExecuteAsync(string path, object stdin = null, CancellationToken token = default(CancellationToken))
        {
            var psi = new ProcessStartInfo(@"python", path)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
            };

            using (var process = new Process() { StartInfo = psi, EnableRaisingEvents = true })
            {
                try
                {
                    process.Start();
                    if (stdin != null)
                    {
                        var jsonIn = JsonConvert.SerializeObject(stdin, Formatting.Indented);
                        await process.StandardInput.WriteAsync(jsonIn);
                        await process.StandardInput.FlushAsync();
                        process.StandardInput.Close();
                    }

                    var output = await process.StandardOutput.ReadToEndAsync();
                    var error = await process.StandardError.ReadToEndAsync();
                    return (output, error);
                }
                finally
                {
                    try
                    {
                        if (!process.HasExited)
                        {
                            process.Kill();
                        }
                    }
                    catch (Exception)
                    {
                        // Deal with it
                    }
                }
            }
        }
    }
}
