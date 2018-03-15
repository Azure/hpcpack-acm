namespace Microsoft.HpcAcm.Frontend.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.HpcAcm.Common.Dto;
    using Microsoft.HpcAcm.Common.Utilities;
    using Microsoft.WindowsAzure.Storage.Blob;

    [Route("api/[controller]")]
    public class TaskOutputController : Controller
    {
        private readonly CloudUtilities utilities;

        public TaskOutputController(CloudUtilities u)
        {
            this.utilities = u;
        }

        // GET: api/taskoutput/getlastpage/2/nodejobresult-node1-0002-0003-0004?pageSize=1024
        [HttpGet("getlastpage/{jobId}/{taskResultKey}")]
        public async Task<TaskOutputPage> GetLastPageAsync(int jobId, string taskResultKey, [FromQuery] int pageSize, CancellationToken token)
        {
            var result = new TaskOutputPage() { Offset = 0, Size = 0 };
            var blob = this.utilities.GetTaskOutputBlob(jobId, taskResultKey);

            if (pageSize > 1024 || !await blob.ExistsAsync(null, null, token))
            {
                return result;
            }

            await blob.FetchAttributesAsync(null, null, null, token);
            if (blob.Properties.Length == 0)
            {
                return result;
            }

            result.Offset = blob.Properties.Length - pageSize;
            if (result.Offset < 0)
            {
                result.Offset = 0;
            }

            using (MemoryStream stream = new MemoryStream(pageSize))
            {
                await blob.DownloadRangeToStreamAsync(stream, result.Offset, pageSize, null, null, null, token);
                stream.Seek(0, SeekOrigin.Begin);
                StreamReader sr = new StreamReader(stream, Encoding.UTF8);
                result.Content = await sr.ReadToEndAsync();
                result.Size = stream.Position;
            }

            return result;
        }

        // GET: api/taskoutput/getpage/2/nodejobresult-node1-0002-0003-0004?pageSize=1024&offset=1024
        [HttpGet("getpage/{jobId}/{taskResultKey}")]
        public async Task<TaskOutputPage> GetPageAsync(int jobId, string taskResultKey, [FromQuery] int pageSize, [FromQuery] int offset, CancellationToken token)
        {
            if (offset < 0) offset = 0;
            var result = new TaskOutputPage() { Offset = offset, Size = 0 };
            var blob = this.utilities.GetTaskOutputBlob(jobId, taskResultKey);

            if (pageSize <= 0) pageSize = 1024;
            if (pageSize > 1024 || !await blob.ExistsAsync(null, null, token))
            {
                return result;
            }

            await blob.FetchAttributesAsync();
            if (blob.Properties.Length <= offset)
            {
                return result;
            }

            using (MemoryStream stream = new MemoryStream(pageSize))
            {
                await blob.DownloadRangeToStreamAsync(stream, offset, pageSize, null, null, null, token);
                stream.Seek(0, SeekOrigin.Begin);
                StreamReader sr = new StreamReader(stream, Encoding.UTF8);
                result.Content = await sr.ReadToEndAsync();
                result.Size = stream.Position;
            }

            return result;
        }

        // GET: api/taskoutput/download/2/nodejobresult-node1-0002-0003-0004
        [HttpGet("download/{jobId}/{taskResultKey}")]
        public async Task<IActionResult> GetDownloadUriAsync(int jobId, string taskResultKey, CancellationToken token)
        {
            var blob = this.utilities.GetTaskOutputBlob(jobId, taskResultKey);

            if (!await blob.ExistsAsync(null, null, token))
            {
                return null;
            }

            if (this.utilities.IsSharedKeyAccount)
            {
                //TODO: test this.
                var sasToken = blob.GetSharedAccessSignature(new SharedAccessBlobPolicy()
                {
                    Permissions = SharedAccessBlobPermissions.Read,
                    SharedAccessExpiryTime = DateTimeOffset.UtcNow + TimeSpan.FromHours(1.0),
                });

                return new RedirectResult(blob.Uri + $"?{sasToken}", false);
            }
            else
            {
                await blob.FetchAttributesAsync(null, null, null, token);
                var stream = await blob.OpenReadAsync(null, null, null, token);
                FileStreamResult r = new FileStreamResult(stream, blob.Properties.ContentType);
                return r;
            }
        }
    }
}
