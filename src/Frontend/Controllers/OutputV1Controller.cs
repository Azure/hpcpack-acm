namespace Microsoft.HpcAcm.Frontend.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using T = System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using Microsoft.HpcAcm.Common.Dto;

    [Route("v1/output")]
    public class OutputV1Controller : Controller
    {
        private readonly DataProvider provider;

        public OutputV1Controller(DataProvider provider)
        {
            this.provider = provider;
        }

        // GET v1/output/clusrun/somekey/raw
        [HttpGet("{jobtype}/{key}/raw")]
        public async T.Task<IActionResult> GetRawAsync(
            JobType jobType,
            string key,
            CancellationToken token = default(CancellationToken))
        {
            return await this.provider.GetOutputRawAsync(jobType, key, token);
        }

        // GET v1/output/clusrun/somekey/page?offset=25&pagesize=100
        [HttpGet("{jobtype}/{key}/page")]
        public async T.Task<IActionResult> GetPageAsync(
            JobType jobType,
            string key,
            [FromQuery] long offset = -DataProvider.MaxPageSize,
            [FromQuery] int pageSize = DataProvider.MaxPageSize,
            CancellationToken token = default(CancellationToken))
        {
            return new OkObjectResult(await this.provider.GetOutputPageAsync(jobType, key, pageSize, offset, token));
        }
    }
}

