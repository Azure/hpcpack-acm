namespace Microsoft.HpcAcm.Frontend.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using T = System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using Microsoft.HpcAcm.Common.Dto;
    using Microsoft.HpcAcm.Common.Utilities;
    using Microsoft.WindowsAzure.Storage.Queue;
    using Microsoft.WindowsAzure.Storage.Table;
    using Newtonsoft.Json;

    [Route("v1/clusrun")]
    public class ClusRunV1Controller : Controller
    {
        private readonly DataProvider provider;

        public ClusRunV1Controller(DataProvider provider)
        {
            this.provider = provider;
        }

        // GET v1/clusrun?lastid=3&count=10
        [HttpGet()]
        public async T.Task<IActionResult> GetJobsAsync([FromQuery] int lastId, [FromQuery] int count = 1000, [FromQuery] bool reverse = false, CancellationToken token = default(CancellationToken))
        {
            return new OkObjectResult(await this.provider.GetJobsAsync(lastId, count, JobType.ClusRun, reverse, token));
        }

        // GET v1/clusrun/5/aggregationresult
        [HttpGet("{jobid}/aggregationresult")]
        public async T.Task<IActionResult> GetJobAggregationResultAsync(
            int jobId,
            CancellationToken token = default(CancellationToken))
        {
            var result = await this.provider.GetJobAggregationResultAsync(jobId, JobType.ClusRun, token);

            if (result == null) return new NotFoundObjectResult("The job hasn't produced any aggregation result.");
            else return new OkObjectResult(result);
        }

        // GET v1/clusrun/5
        [HttpGet("{jobid}")]
        public async T.Task<IActionResult> GetJobAsync(int jobId, CancellationToken token = default(CancellationToken))
        {
            var j = await this.provider.GetJobAsync(jobId, JobType.ClusRun, token);
            return j == null ? (IActionResult)new NotFoundResult() : new OkObjectResult(j);
        }

        // GET v1/clusrun/5/tasks?lastid=5&count=10&requeueCount=0
        [HttpGet("{jobid}/tasks")]
        public async T.Task<IActionResult> GetJobTasksAsync(
            int jobId,
            [FromQuery] int lastId = 0,
            [FromQuery] int count = 1000,
            [FromQuery] int requeueCount = 0,
            CancellationToken token = default(CancellationToken))
        {
            var tasks = await this.provider.GetTasksAsync(
                jobId,
                requeueCount,
                lastId,
                count,
                JobType.ClusRun,
                token);

            return new OkObjectResult(tasks);
        }

        // GET v1/clusrun/5/tasks/10?requeueCount=0
        [HttpGet("{jobid}/tasks/{taskid}")]
        public async T.Task<IActionResult> GetJobTaskAsync(
            int jobId,
            int taskId = 0,
            [FromQuery] int requeueCount = 0,
            CancellationToken token = default(CancellationToken))
        {
            var task = await this.provider.GetTaskAsync(
                jobId,
                requeueCount,
                taskId,
                JobType.ClusRun,
                token);

            return task == null ? (IActionResult)new NotFoundResult() : new OkObjectResult(task);
        }

        // GET v1/diagnostics/5/tasks/10/result?requeueCount=0
        [HttpGet("{jobid}/tasks/{taskid}/result")]
        public async T.Task<IActionResult> GetJobTaskResultAsync(
            int jobId,
            int taskId = 0,
            [FromQuery] int requeueCount = 0,
            CancellationToken token = default(CancellationToken))
        {
            var task = await this.provider.GetTaskResultAsync(
                jobId,
                requeueCount,
                taskId,
                JobType.ClusRun,
                token);

            return task == null ? (IActionResult)new NotFoundResult() : new OkObjectResult(task);
        }

        // POST v1/clusrun
        [HttpPost()]
        public async T.Task<IActionResult> CreateJobAsync([FromBody] Job job, CancellationToken token)
        {
            job.Type = JobType.ClusRun;
            if (job.CommandLine == null)
            {
                return new BadRequestObjectResult("The CommandLine field should be specified.");
            }

            if (job.TargetNodes == null || job.TargetNodes.Length == 0)
            {
                return new BadRequestObjectResult("The TargetNodes shouldn't be empty.");
            }

            job = await this.provider.CreateJobAsync(job, token);
            return new CreatedResult($"/v1/clusrun/{job.Id}", job);
        }

        // PATCH v1/clusrun/5
        [HttpPatch("{jobid}")]
        public async T.Task<IActionResult> PatchJobAsync(int jobId, [FromBody] Job job, CancellationToken token)
        {
            job.Id = jobId;
            job.Type = JobType.ClusRun;
            return await this.provider.PatchJobAsync(job, token);
        }

        [HttpGet("testcanceljob/{id}")]
        public async T.Task<IActionResult> TestCancelJobAsync(int id, CancellationToken token)
        {
            var job = new Job()
            {
                Request = "cancel",
                Type = JobType.ClusRun,
                Id = id,
            };

            return await this.provider.PatchJobAsync(job, token);
        }

        [HttpGet("testnewjob/{nodes}")]
        public async T.Task<IActionResult> TestCreateJobAsync(string nodes, CancellationToken token)
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

            job = await this.provider.CreateJobAsync(job, token);
            return new CreatedResult($"/v1/clusrun/{job.Id}", job);
        }
    }
}

