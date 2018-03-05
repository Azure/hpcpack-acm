namespace Microsoft.HpcAcm.Services.NodeReporter.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.HpcAcm.Common.Dto;

    [Route("api/[controller]")]
    public class DiagnosticsController : Controller
    {
        // GET api/diagnostics/tests
        [HttpGet("tests")]
        public async Task<IEnumerable<DiagnosticsTest>> GetDiagnosticsTestsAsync()
        {
            await Task.CompletedTask;
            return new DiagnosticsTest[] { new DiagnosticsTest() };
        }

        // GET api/diagnostics/jobs
        [HttpGet("jobs")]
        public async Task<IEnumerable<DiagnosticsJob>> GetDiagnosticsJobsAsync()
        {
            await Task.CompletedTask;
            return new DiagnosticsJob[] { new DiagnosticsJob() };
        }

        // GET api/diagnostics/jobs/5
        [HttpGet("jobs/{jobid}")]
        public async Task<JobResult> GetAsync(int jobId)
        {
            await Task.CompletedTask;
            return new JobResult(); 
        }
        
        // GET api/diagnostics/statistics
        [HttpGet("statistics")]
        public async Task<IDictionary<JobState, int>> GetHistoryAsync()
        {
            await Task.CompletedTask;
            return new Dictionary<JobState, int>();
        }
        
        // POST api/diagnostics/newjob
        [HttpPost("newjob")]
        public async Task<int> NewJob([FromBody] DiagnosticsJob job)
        {
            await Task.CompletedTask;
            return 1;
        }
        
        // POST api/diagnostics/jobs/5/rerun
        [HttpPost("jobs/{jobid}/{operation}")]
        public async Task TakeAction(int jobId, string operation)
        {
            await Task.CompletedTask;
        }
    }
}

