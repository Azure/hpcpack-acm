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
        private readonly ILogger logger;
        private readonly CloudUtilities utilities;

        public ClusRunController(ILogger<ClusRunController> logger, CloudUtilities cloudEntities)
        {
            this.logger = logger;
            this.utilities = cloudEntities;
        }

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

        [HttpGet("testnewjob")]
        public async Task<int> TestNewJobAsync(CancellationToken token)
        {
            var job = new Job()
            {
                CommandLine = "cat /opt/hpcnodemanager/nodemanager.json",
                Name = "testjob",
                RequeueCount = 0,
                State = JobState.Queued,
                TargetNodes = new string[] { "evanc6" },
                Type = JobType.ClusRun,
            };

            return await this.NewJobAsync(job, token);
        }

        // POST api/clusrun
        [HttpPost()]
        public async Task<int> NewJobAsync([FromBody] Job job, CancellationToken token)
        {
            this.logger.LogInformation("New clusrun job called. creating job");
            var jobTable = this.utilities.GetJobsTable();

            job.Id = await this.utilities.GetNextId("Jobs", $"{job.Type}", token);
            this.logger.LogInformation("generated new job id {0}", job.Id);

            var partitionName = utilities.GetJobPartitionKey(job.Id, $"{job.Type}");
            var rowKey = utilities.JobEntryKey;

            var result = await jobTable.ExecuteAsync(
                TableOperation.Insert(new JsonTableEntity(partitionName, rowKey, job)),
                null, null, token);

            this.logger.LogInformation("create job result {0}", result.HttpStatusCode);

            HttpResponseMessage response = new HttpResponseMessage((HttpStatusCode)result.HttpStatusCode);
            response.EnsureSuccessStatusCode();

            this.logger.LogInformation("Creating job dispatch message");
            var jobDispatchQueue = await this.utilities.GetOrCreateJobDispatchQueueAsync(token);

            var jobMsg = new JobDispatchMessage() { Id = job.Id, Type = job.Type };
            await jobDispatchQueue.AddMessageAsync(new CloudQueueMessage(JsonConvert.SerializeObject(jobMsg)), null, null, null, null, token);
            this.logger.LogInformation("Create job dispatch message success.");

            return job.Id;
        }

        // POST api/clusrun/jobs/5/rerun
        [HttpPost("{jobid}/{operation}")]
        public async Task TakeAction(int jobId, string operation)
        {
            await Task.CompletedTask;
        }
    }
}

