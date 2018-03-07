namespace Microsoft.HpcAcm.Frontend.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.HpcAcm.Common.Dto;

    [Route("api/[controller]")]
    public class ClusRunController : Controller
    {
        // GET api/clusrun
        [HttpGet()]
        public async Task<IEnumerable<Job>> GetClusRunJobsAsync()
        {
            await Task.CompletedTask;
            return new Job[] { new Job() };
        }

        // GET api/clusrun/5
        [HttpGet("{jobid}")]
        public async Task<JobResult> GetAsync(int jobId)
        {
            await Task.CompletedTask;
            return new JobResult();
        }

        // POST api/clusrun
        [HttpPost()]
        public async Task<int> NewJob([FromBody] Job job)
        {
            await Task.CompletedTask;
            return 1;
        }

        // POST api/clusrun/jobs/5/rerun
        [HttpPost("{jobid}/{operation}")]
        public async Task TakeAction(int jobId, string operation)
        {
            await Task.CompletedTask;
        }
    }
}

