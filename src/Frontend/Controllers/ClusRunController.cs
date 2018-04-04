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

        // GET api/clusrun?lastJobId=3&jobCount=10
        [HttpGet()]
        public async Task<IEnumerable<Job>> GetClusRunJobsAsync([FromQuery] int? lastJobId, [FromQuery] int? jobCount, CancellationToken token)
        {
            this.logger.LogInformation("Get clusrun jobs called, lastId {0}, jobCount {1}", lastJobId, jobCount);
            var jobTable = this.utilities.GetJobsTable();

            var lowJobPartitionKey = this.utilities.GetJobPartitionKey($"{JobType.ClusRun}", lastJobId ?? 0);
            var rowKey = utilities.JobEntryKey;

            var q = new TableQuery<JsonTableEntity>()
                .Where(TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition(CloudUtilities.PartitionKeyName, QueryComparisons.GreaterThan, lowJobPartitionKey),
                    TableOperators.And,
                    TableQuery.GenerateFilterCondition(CloudUtilities.RowKeyName, QueryComparisons.Equal, rowKey)))
                .Take(jobCount);

            var jobs = new List<Job>(jobCount ?? 1000);
            TableContinuationToken conToken = null;

            do
            {
                var queryResult = await jobTable.ExecuteQuerySegmentedAsync(q, conToken, null, null, token);

                jobs.AddRange(queryResult.Results.Select(r => r.GetObject<Job>()));

                conToken = queryResult.ContinuationToken;
            }
            while (conToken != null);

            return jobs;
        }

        // GET api/clusrun/5?lastNodeName=abc&nodeCount=10
        [HttpGet("{jobid}")]
        public async Task<JobResult> GetAsync(int jobId, [FromQuery] string lastNodeName, [FromQuery] int? nodeCount, CancellationToken token)
        {
            this.logger.LogInformation("Get clusrun job called. getting job");
            var jobTable = this.utilities.GetJobsTable();

            var jobPartitionKey = this.utilities.GetJobPartitionKey($"{JobType.ClusRun}", jobId);
            var rowKey = utilities.JobEntryKey;

            var result = await jobTable.ExecuteAsync(
                TableOperation.Retrieve<JsonTableEntity>(jobPartitionKey, rowKey),
                null, null, token);

            this.logger.LogInformation("Retrive job {0} status code {1}", jobId, result.HttpStatusCode);

            HttpResponseMessage response = new HttpResponseMessage((HttpStatusCode)result.HttpStatusCode);
            response.EnsureSuccessStatusCode();

            if (result.Result == null)
            {
                return null;
            }

            JobResult j = ((JsonTableEntity)result.Result).GetObject<JobResult>();

            this.logger.LogInformation("Fetching job {0} output", jobId);

            var lowResultKey = this.utilities.GetJobResultKey(lastNodeName, null);
            var highResultKey = this.utilities.GetMaximumJobResultKey();
            var partitionQuery = this.utilities.GetPartitionQueryString(jobPartitionKey);
            var rowKeyRangeQuery = this.utilities.GetRowKeyRangeString(lowResultKey, highResultKey);

            var q = new TableQuery<JsonTableEntity>()
                .Where(TableQuery.CombineFilters(partitionQuery, TableOperators.And, rowKeyRangeQuery))
                .Take(nodeCount);

            TableContinuationToken conToken = null;

            j.Results = new List<NodeResult>(nodeCount ?? 1000);

            var taskInfos = new List<(string, ComputeNodeTaskCompletionEventArgs)>();

            do
            {
                var queryResult = await jobTable.ExecuteQuerySegmentedAsync(q, conToken, null, null, token);

                taskInfos.AddRange(queryResult.Results.Select(r => (r.RowKey, r.GetObject<ComputeNodeTaskCompletionEventArgs>())));

                conToken = queryResult.ContinuationToken;
            }
            while (conToken != null);

            j.Results = taskInfos.GroupBy(t => t.Item2.NodeName.ToLowerInvariant()).Select(g => new NodeResult()
            {
                NodeName = g.Key,
                JobId = jobId,
                Results = g.Select(e => new CommandResult()
                {
                    CommandLine = j.CommandLine,
                    NodeName = g.Key,
                    ResultKey = e.Item1,
                    TaskInfo = e.Item2.TaskInfo,
                    Test = j.DiagnosticTest,
                }).ToList(),
            }).ToList();

            return j;
        }

        [HttpGet("testnewdiagjob")]
        public async Task<int> TestNewDiagJobAsync(CancellationToken token)
        {
            var job = new Job()
            {
                DiagnosticTest = new DiagnosticsTest() { Category = "test", Name = "test" },
                Name = "test-diag",
                RequeueCount = 0,
                State = JobState.Queued,
                TargetNodes = new string[] { "evanc6", "evanclinuxdev", "testnode1", "testnode2" },
                Type = JobType.Diagnostics,
            };

            return await this.NewJobAsync(job, token);
        }

        [HttpGet("testnewclusrunjob")]
        public async Task<int> TestNewJobAsync(CancellationToken token)
        {
            var job = new Job()
            {
                CommandLine = "hostname",
                Name = "hostname",
                RequeueCount = 0,
                State = JobState.Queued,
                TargetNodes = new string[] { "evanc6", "evanclinuxdev", "testnode1", "testnode2" },
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

            var partitionName = utilities.GetJobPartitionKey($"{job.Type}", job.Id);
            var rowKey = utilities.JobEntryKey;

            var result = await jobTable.ExecuteAsync(
                TableOperation.Insert(new JsonTableEntity(partitionName, rowKey, job)),
                null, null, token);

            this.logger.LogInformation("create job result {0}", result.HttpStatusCode);

            HttpResponseMessage response = new HttpResponseMessage((HttpStatusCode)result.HttpStatusCode);
            response.EnsureSuccessStatusCode();

            this.logger.LogInformation("Creating job dispatch message");
            var jobDispatchQueue = this.utilities.GetJobDispatchQueue();

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

