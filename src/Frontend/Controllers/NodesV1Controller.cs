namespace Microsoft.HpcAcm.Frontend.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using T = System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Serilog;
    using Microsoft.HpcAcm.Common.Dto;
    using Microsoft.HpcAcm.Common.Utilities;
    using Microsoft.WindowsAzure.Storage.Table;
    using Newtonsoft.Json;
    using Microsoft.AspNetCore.Authorization;

    [Authorize]
    [Route("v1/nodes")]
    public class NodesV1Controller : Controller
    {
        private readonly DataProvider provider;

        public NodesV1Controller(DataProvider provider)
        {
            this.provider = provider;
        }

        // GET v1/nodes?count=50&lastid=testnode
        [HttpGet()]
        public async T.Task<IActionResult> GetAsync(
            [FromQuery] string lastId,
            [FromQuery] int count = 100,
            CancellationToken token = default(CancellationToken))
        {
            var nodes = await this.provider.GetNodesAsync(lastId, count, token);
            return new OkObjectResult(nodes);
        }

        // GET v1/nodes/node1
        [HttpGet("{id}")]
        public async T.Task<IActionResult> GetAsync(string id, CancellationToken token)
        {
            var node = await this.provider.GetNodeAsync(id, token);

            return node == null ? (IActionResult)new NotFoundResult() : new OkObjectResult(node);
        }

        // GET v1/nodes/node1/events
        [HttpGet("{id}/events")]
        public async T.Task<IActionResult> GetEventsAsync(string id, CancellationToken token)
        {
            var events = await this.provider.GetNodeEventsAsync(id, token);
            return new OkObjectResult(events);
        }

        // GET v1/nodes/node1/jobs
        [HttpGet("{id}/jobs")]
        public async T.Task<IActionResult> GetJobsAsync(string id, CancellationToken token)
        {
            var jobs = await this.provider.GetNodeJobInfoAsync(id, token);
            return jobs == null ? (IActionResult)new NotFoundObjectResult("Cannot find the node jobs information") : new OkObjectResult(jobs);
        }

        // GET v1/nodes/node1/metrichistory
        [HttpGet("{id}/metrichistory")]
        public async T.Task<IActionResult> GetHistoryAsync(string id, CancellationToken token)
        {
            var history = await this.provider.GetNodeMetricHistoryAsync(id, token);
            return new OkObjectResult(history);
        }

        // GET v1/nodes/node1/metadata
        [HttpGet("{id}/metadata")]
        public async T.Task<IActionResult> GetMetadataAsync(string id, CancellationToken token)
        {
            var jsonString = (string)await this.provider.GetNodeMetadataAsync(id, token);
            return jsonString == null ? (IActionResult)new NotFoundResult() : new OkObjectResult(JsonConvert.DeserializeObject(jsonString));
        }

        // GET v1/nodes/longquery
        [HttpGet("longquery")]
        public async T.Task<IActionResult> GetScheduledEventsAsync(CancellationToken token)
        {
            await T.Task.Delay(100000, token);
            return new OkObjectResult("long query result. good");
        }

        // GET v1/nodes/node1/scheduledevents
        [HttpGet("{id}/scheduledevents")]
        public async T.Task<IActionResult> GetScheduledEventsAsync(string id, CancellationToken token)
        {
            var jsonString = (string)await this.provider.GetNodeScheduledEventsAsync(id, token);
            return jsonString == null ? (IActionResult)new NotFoundResult() : new OkObjectResult(JsonConvert.DeserializeObject(jsonString));
        }
    }
}
