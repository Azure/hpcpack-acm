namespace Frontend.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using Microsoft.HpcAcm.Common.Dto;
    using Microsoft.HpcAcm.Common.Utilities;
    using Microsoft.WindowsAzure.Storage.Table;

    [Produces("application/json")]
    [Route("api/[controller]")]
    public class JobsController : Controller
    {
        private readonly ILogger logger;
        private readonly CloudUtilities utilities;

        public JobsController(ILogger<JobsController> logger, CloudUtilities cloudEntities)
        {
            this.logger = logger;
            this.utilities = cloudEntities;
        }

        // GET api/jobs?lastid=3&count=10&type=diagnostics
        [HttpGet()]
        public async Task<IEnumerable<Job>> GetClusRunJobsAsync(
            [FromQuery] int? lastId, 
            [FromQuery] int count = 1000, 
            [FromQuery] JobType type = JobType.ClusRun,
            CancellationToken token = default(CancellationToken))
        {
            this.logger.LogInformation("Get {type} jobs called, lastId {lastId}, jobCount {count}", type, lastId, count);
            var jobTable = this.utilities.GetJobsTable();

            var lowJobPartitionKey = this.utilities.GetJobPartitionKey($"{type}", lastId ?? 0);
            var rowKey = utilities.JobEntryKey;

            var q = new TableQuery<JsonTableEntity>()
                .Where(TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition(CloudUtilities.PartitionKeyName, QueryComparisons.GreaterThan, lowJobPartitionKey),
                    TableOperators.And,
                    TableQuery.GenerateFilterCondition(CloudUtilities.RowKeyName, QueryComparisons.Equal, rowKey)))
                .Take(count);

            var jobs = new List<Job>(count);
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
    }
}