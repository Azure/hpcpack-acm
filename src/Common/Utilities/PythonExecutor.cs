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
        public static async Task<ProcessResult> ExecuteAsync(CloudBlob blob, object stdin = null, CancellationToken token = default(CancellationToken))
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

        public static async Task<ProcessResult> ExecuteScriptAsync(string script, object stdin = null, CancellationToken token = default(CancellationToken))
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

        public static async Task<ProcessResult> ExecuteAsync(string path, object stdin = null, CancellationToken token = default(CancellationToken))
        {
            return await AsyncProcess.RunAsync("python", path, stdin, 30, token);
        }
    }
}
