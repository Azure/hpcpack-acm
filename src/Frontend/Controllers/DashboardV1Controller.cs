namespace Microsoft.HpcAcm.Frontend.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using T = System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using Microsoft.HpcAcm.Common.Dto;
    using Microsoft.HpcAcm.Common.Utilities;
    using Microsoft.WindowsAzure.Storage.Table;
    using Newtonsoft.Json;

    [Route("v1/dashboard")]
    public class DashboardV1Controller : Controller
    {
        private readonly DataProvider provider;

        public DashboardV1Controller(DataProvider provider)
        {
            this.provider = provider;
        }

        // GET v1/dashboard/nodes
        [HttpGet("nodes")]
        public async T.Task<IActionResult> GetNodesAsync(
            CancellationToken token = default(CancellationToken))
        {
            var data = await this.provider.GetDashboardNodesAsync(token);
            return data == null ? (IActionResult)new NotFoundObjectResult("No dashboard data found.") : new OkObjectResult(data);
        }

        // GET v1/dashboard/diagnostics
        [HttpGet("diagnostics")]
        public async T.Task<IActionResult> GetDiagnosticsAsync(
            CancellationToken token = default(CancellationToken))
        {
            var data = await this.provider.GetDashboardDiagnosticsAsync(token);
            return data == null ? (IActionResult)new NotFoundObjectResult("No dashboard data found.") : new OkObjectResult(data);
        }

        // GET v1/dashboard/clusrun
        [HttpGet("clusrun")]
        public async T.Task<IActionResult> GetClusrunAsync(
            CancellationToken token = default(CancellationToken))
        {
            var data = await this.provider.GetDashboardClusrunAsync(token);
            return data == null ? (IActionResult)new NotFoundObjectResult("No dashboard data found.") : new OkObjectResult(data);
        }
    }
}
