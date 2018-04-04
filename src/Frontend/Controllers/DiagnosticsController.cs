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
        private readonly CloudUtilities utilities;
        public DiagnosticsController(CloudUtilities utilities)
        {
            this.utilities = utilities;
        }

        // GET api/diagnostics
        [HttpGet()]
        public async Task<IEnumerable<DiagnosticsTest>> GetDiagnosticsTestsAsync(CancellationToken token)
        {
            var jobsTable = this.utilities.GetJobsTable();

            var partitionString = this.utilities.GetPartitionKeyRangeString(
                this.utilities.GetDiagPartitionKey(this.utilities.MinString),
                this.utilities.GetDiagPartitionKey(this.utilities.MaxString));

            TableContinuationToken conToken = null;
            List<DiagnosticsTest> tests = new List<DiagnosticsTest>();

            var q = new TableQuery<JsonTableEntity>().Where(partitionString);
            q.SelectColumns = new List<string>() { CloudUtilities.PartitionKeyName, CloudUtilities.RowKeyName };

            do
            {
                var result = await jobsTable.ExecuteQuerySegmentedAsync(q, conToken, null, null, token);

                tests.AddRange(result.Results.Select(e => new DiagnosticsTest()
                {
                    Category = this.utilities.GetDiagCategoryName(e.PartitionKey),
                    Name = e.RowKey
                }));

                conToken = result.ContinuationToken;
            }
            while (conToken != null);

            return tests;
        }
    }
}

