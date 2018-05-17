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

    [Route("api/[controller]")]
    public class NodesController : Controller
    {
        private readonly DataProvider provider;

        public NodesController(DataProvider provider)
        {
            this.provider = provider;
        }

        // GET api/nodes?count=50&lastid=testnode
        [HttpGet()]
        public T.Task<IEnumerable<Node>> GetAsync(
            [FromQuery] string lastId,
            [FromQuery] int count = 1000,
            CancellationToken token = default(CancellationToken))
        {
            return this.provider.GetNodesAsync(lastId, count, token);
        }

        // GET api/nodes/node1
        [HttpGet("{id}")]
        public async T.Task<IActionResult> GetAsync(string id, CancellationToken token)
        {
            var node = await this.provider.GetNodeAsync(id, token);

            return node == null ? (IActionResult)new NotFoundResult() : new OkObjectResult(node);
        }

        // POST api/nodes/node1/online
        [HttpPost("{name}/{operation}")]
        public async T.Task TakeAction(string name, string operation, [FromBody]string value)
        {
            await System.Threading.Tasks.Task.CompletedTask;
        }

        // POST api/nodes
        [HttpPost]
        public async T.Task<IDictionary<DateTime, IDictionary<NodeHealth, int>>> GetHistoryAsync([FromBody] int hours)
        {
            await System.Threading.Tasks.Task.CompletedTask;
            return new Dictionary<DateTime, IDictionary<NodeHealth, int>>();
        }
    }
}
