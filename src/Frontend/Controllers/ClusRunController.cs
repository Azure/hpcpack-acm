namespace Microsoft.HpcAcm.Frontend.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using Microsoft.HpcAcm.Common.Dto;
    using Microsoft.HpcAcm.Common.Utilities;
    using Microsoft.WindowsAzure.Storage.Queue;
    using Microsoft.WindowsAzure.Storage.Table;
    using Newtonsoft.Json;

    [Route("api/[controller]")]
    public class ClusRunController : Controller
    {
        private readonly DataProvider provider;

        public ClusRunController(DataProvider provider)
        {
            this.provider = provider;
        }

        // GET api/clusrun?lastid=3&count=10
        [HttpGet()]
        public Task<IEnumerable<Job>> GetClusRunJobsAsync([FromQuery] int lastId, [FromQuery] int count = 1000, CancellationToken token = default(CancellationToken))
        {
            return this.provider.GetJobsAsync(lastId, count, JobType.ClusRun, token);
        }

        // GET api/clusrun/5?lastNodeName=abc&nodeCount=10
        [HttpGet("{jobid}")]
        public Task<JobResult> GetClusRunJobAsync(int jobId, [FromQuery] string lastNodeName, [FromQuery] int nodeCount = 1000, CancellationToken token = default(CancellationToken))
        {
            return this.provider.GetJobAsync(jobId, lastNodeName, nodeCount, JobType.ClusRun, token);
        }

        // GET api/clusrun/5/results/resultkey?offset=25&pagesize=100&raw=true
        [HttpGet("{jobid}/results/{resultkey}")]
        public async Task<IActionResult> GetClusRunResultAsync(
            int jobId,
            string resultKey,
            [FromQuery] long offset = -1000,
            [FromQuery] int pageSize = 1000,
            [FromQuery] bool raw = false,
            CancellationToken token = default(CancellationToken))
        {
            if (raw)
            {
                return await this.provider.GetOutputRawAsync(JobType.ClusRun, jobId, resultKey, token);
            }
            else
            {
                return new OkObjectResult(await this.provider.GetOutputPageAsync(JobType.ClusRun, jobId, resultKey, pageSize, offset, token));
            }
        }

        [HttpGet("testnewjob/{nodes}")]
        public async Task<int> TestCreateJobAsync(string nodes, CancellationToken token)
        {
            var job = new Job()
            {
                CommandLine = "hostname",
                Name = "hostname",
                RequeueCount = 0,
                State = JobState.Queued,
                TargetNodes = nodes.Split(','),
                Type = JobType.ClusRun,
            };

            return await this.provider.CreateJobAsync(job, token);
        }

        // POST api/clusrun
        [HttpPost()]
        public async Task<IActionResult> CreateJobAsync([FromBody] Job job, CancellationToken token)
        {
            job.Type = JobType.ClusRun;
            int id = await this.provider.CreateJobAsync(job, token);
            return new CreatedResult($"/api/clusrun/{id}", null);
        }

        // POST api/clusrun/jobs/5/rerun
        [HttpPost("{jobid}/{operation}")]
        public async Task TakeAction(int jobId, string operation)
        {
            await Task.CompletedTask;
        }
    }
}

