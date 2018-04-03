namespace Microsoft.HpcAcm.Common.Utilities
{
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;

    public static class PythonExecutor
    {
        public static async Task<(string, string)> ExecuteAsync(string script, object stdin = null)
        {
            var psi = new System.Diagnostics.ProcessStartInfo(
                @"python",
                $"-c \"{script.Replace("\"", "\\\"")}\"")
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
                    catch(Exception)
                    {
                        // Deal with it
                    }
                }
            }
        }
    }
}
