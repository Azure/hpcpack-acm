namespace Microsoft.HpcAcm.Services.NodeReporter.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.HpcAcm.Common.Dto;

    [Route("api/[controller]")]
    public class ClusRunController: Controller
    {
        // GET api/clusrun/jobs
        [HttpGet("jobs")]
        public async Task<IEnumerable<ClusRunJob>> GetClusRunJobsAsync()
        {
            await Task.CompletedTask;
            return new ClusRunJob[] { new ClusRunJob() };
        }

        // GET api/clusrun/jobs/5
        [HttpGet("jobs/{jobid}")]
        public async Task<JobResult> GetAsync(int jobId)
        {
            await Task.CompletedTask;
            return new JobResult(); 
        }
        
        // POST api/clusrun/newjob
        [HttpPost("newjob")]
        public async Task<int> NewJob([FromBody] ClusRunJob job)
        {
            await Task.CompletedTask;
            return 1;
        }
        
        // POST api/clusrun/jobs/5/rerun
        [HttpPost("jobs/{jobid}/{operation}")]
        public async Task TakeAction(int jobId, string operation)
        {
            await Task.CompletedTask;
        }
    }
}

