namespace frontend.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using Microsoft.HpcAcm.Common.Dto;
    using Microsoft.HpcAcm.Common.Utilities;
    using Microsoft.HpcAcm.Services.Common;
    using Microsoft.WindowsAzure.Storage.Queue;
    using Microsoft.WindowsAzure.Storage.Table;
    using Newtonsoft.Json;

    [Route("api/[controller]")]
    public class ValuesController : Controller
    {
        private readonly CloudUtilities utilities;
        private readonly ILogger logger;
        public ValuesController(CloudUtilities utilities, ILogger<ValuesController> logger)
        {
            this.utilities = utilities;
            this.logger = logger;
        }
        // GET api/values
        [HttpGet()]
        public async Task<IEnumerable<string>> GetAsync(CancellationToken token)
        {
            await Task.CompletedTask;
            return new string[] { "value1", "value2" };
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public async Task<string> GetAsync(int id, CancellationToken token)
        {
            this.logger.LogInformation("GetAsync called. creating job");
            var jobTable = await this.utilities.GetOrCreateJobsTableAsync(token);
            var job = new Job()
            {
                CommandLine = "cat /opt/hpcnodemanager/nodemanager.json",
                Id = id,
                Name = "testjob",
                RequeueCount = 0,
                State = JobState.Queued,
                TargetNodes = new string[] { "evanclinuxdev" },
                Type = JobType.ClusRun,
            };

            var partitionName = utilities.GetJobPartitionName(job.Id, $"{job.Type}");
            var rowKey = utilities.JobEntryKey;

            var result = await jobTable.ExecuteAsync(
                TableOperation.Insert(new JsonTableEntity(partitionName, rowKey, job)),
                null, null, token);

            this.logger.LogInformation("GetAsync called. create job result {0}", result.HttpStatusCode);

            this.logger.LogInformation("GetAsync called. Creating job dispatch message");
            var jobDispatchQueue = await this.utilities.GetOrCreateJobDispatchQueueAsync(token);

            var jobMsg = new JobDispatchMessage() { Id = id, Type = JobType.ClusRun };
            await jobDispatchQueue.AddMessageAsync(new CloudQueueMessage(JsonConvert.SerializeObject(jobMsg)), null, null, null, null, token);
            this.logger.LogInformation("GetAsync called. Create job dispatch message success.");

            return "value";
        }

        // POST api/values
        [HttpPost]
        public void Post([FromBody]string value)
        {
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
