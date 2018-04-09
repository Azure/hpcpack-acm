namespace Microsoft.HpcAcm.Frontend.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.HpcAcm.Common.Dto;
    using Microsoft.HpcAcm.Common.Utilities;
    using Microsoft.WindowsAzure.Storage.Table;

    [Route("api/[controller]")]
    public class DiagnosticsController : Controller
    {
        private readonly DataProvider provider;

        public DiagnosticsController(DataProvider provider)
        {
            this.provider = provider;
        }

        // GET api/diagnostics/tests
        [HttpGet("tests")]
        public Task<IEnumerable<DiagnosticsTest>> GetDiagnosticsTestsAsync(CancellationToken token)
        {
            return this.provider.GetDiagnosticsTestsAsync(token);
        }

        // GET api/diagnostics?lastid=3&count=10
        [HttpGet()]
        public Task<IEnumerable<Job>> GetDiagnosticsJobsAsync([FromQuery] int lastId, [FromQuery] int count = 1000, CancellationToken token = default(CancellationToken))
        {
            return this.provider.GetJobsAsync(lastId, count, JobType.Diagnostics, token);
        }

        // GET api/diagnostics/5/tasks?lastid=0&count=10&requeueCount=0
        [HttpGet("{jobid}/tasks")]
        public Task<IEnumerable<ComputeNodeTaskCompletionEventArgs>> GetDiagnosticsTasksAsync(int jobId, [FromQuery] int lastId, [FromQuery] int count = 1000, [FromQuery] int requeueCount = 0, CancellationToken token = default(CancellationToken))
        {
            return this.provider.GetTasksAsync(jobId, requeueCount, lastId, count, JobType.Diagnostics, token);
        }

        // GET api/diagnostics/5?lastNodeName=abc&nodeCount=10
        [HttpGet("{jobid}")]
        public Task<JobResult> GetDiagnosticsJobAsync(int jobId, [FromQuery] string lastNodeName, [FromQuery] int nodeCount = 1000, CancellationToken token = default(CancellationToken))
        {
            return this.provider.GetJobAsync(jobId, lastNodeName, nodeCount, JobType.Diagnostics, token);
        }

        // GET api/diagnostics/5/results/resultkey?offset=25&pagesize=100&raw=true
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
                return await this.provider.GetOutputRawAsync(JobType.Diagnostics, jobId, resultKey, token);
            }
            else
            {
                return new OkObjectResult(await this.provider.GetOutputPageAsync(JobType.Diagnostics, jobId, resultKey, pageSize, offset, token));
            }
        }

        [HttpGet("testnewjob")]
        public async Task<int> TestCreateJobAsync(CancellationToken token)
        {
            var job = new Job()
            {
                Name = "diag-test-test",
                RequeueCount = 0,
                State = JobState.Queued,
                TargetNodes = new string[] { "evanc6", "evanclinuxdev", "testnode1", "testnode2" },
                Type = JobType.Diagnostics,
                DiagnosticTest = new DiagnosticsTest() { Category = "test", Name = "test" }
            };

            return await this.provider.CreateJobAsync(job, token);
        }

        // POST api/clusrun/jobs/5/rerun
        [HttpPost("{jobid}/{operation}")]
        public async Task TakeAction(int jobId, string operation)
        {
            await Task.CompletedTask;
        }
    }
}

