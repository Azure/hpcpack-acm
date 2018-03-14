namespace Microsoft.HpcAcm.Frontend.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
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
            var blob = this.utilities.GetTaskOutputBlob(jobId, taskResultKey);

            if (pageSize > 1024 || !await blob.ExistsAsync(null, null, token))
            {
                return new TaskOutputPage() { Offset = 0, Size = 0 };
            }

            await blob.FetchAttributesAsync(null, null, null, token);

            var result = new TaskOutputPage() { Offset = blob.Properties.Length - pageSize };
            if (result.Offset < 0)
            {
                result.Offset = 0;
            }

            using (MemoryStream stream = new MemoryStream(pageSize))
            {
                await blob.DownloadRangeToStreamAsync(stream, result.Offset, pageSize, null, null, null, token);
                result.Content = stream.ToArray();
                result.Size = stream.Length;
            }

            return result;
        }

        // GET: api/taskoutput/getpage/2/nodejobresult-node1-0002-0003-0004?pageSize=1024&offset=1024
        [HttpGet("getpage/{jobId}/{taskResultKey}")]
        public async Task<TaskOutputPage> GetPageAsync(int jobId, string taskResultKey, [FromQuery] int pageSize, [FromQuery] int offset, CancellationToken token)
        {
            var blob = this.utilities.GetTaskOutputBlob(jobId, taskResultKey);

            if (pageSize > 1024 || !await blob.ExistsAsync(null, null, token))
            {
                return new TaskOutputPage() { Offset = offset, Size = 0 };
            }

            var result = new TaskOutputPage() { Offset = offset, };

            using (MemoryStream stream = new MemoryStream(pageSize))
            {
                await blob.DownloadRangeToStreamAsync(stream, offset, pageSize, null, null, null, token);
                result.Content = stream.ToArray();
                result.Size = stream.Length;
            }

            return result;
        }

        // GET: api/taskoutput/getdownloaduri/2/nodejobresult-node1-0002-0003-0004
        [HttpGet("getdownloaduri/{jobId}/{taskResultKey}")]
        public async Task<string> GetDownloadUriAsync(int jobId, string taskResultKey, CancellationToken token)
        {
            var blob = this.utilities.GetTaskOutputBlob(jobId, taskResultKey);

            if (!await blob.ExistsAsync(null, null, token))
            {
                return null;
            }

            var sasToken = blob.GetSharedAccessSignature(new SharedAccessBlobPolicy()
            {
                Permissions = SharedAccessBlobPermissions.Read,
                SharedAccessExpiryTime = DateTimeOffset.UtcNow + TimeSpan.FromHours(1.0),
            });

            return blob.Uri + $"?{sasToken}";
        }
    }
}
