namespace Microsoft.HpcAcm.Frontend.Controllers
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
        public Task<IEnumerable<Node>> GetAsync(
            [FromQuery] string lastId,
            [FromQuery] int count = 1000,
            CancellationToken token = default(CancellationToken))
        {
            return this.provider.GetNodesAsync(lastId, count, token);
        }

        // GET api/nodes/node1
        [HttpGet("{id}")]
        public Task<IActionResult> GetAsync(string id, CancellationToken token)
        {
            return this.provider.GetNodeAsync(id, token);
        }

        // POST api/nodes/node1/online
        [HttpPost("{name}/{operation}")]
        public async Task TakeAction(string name, string operation, [FromBody]string value)
        {
            await Task.CompletedTask;
        }

        // POST api/nodes
        [HttpPost]
        public async Task<IDictionary<DateTime, IDictionary<NodeHealth, int>>> GetHistoryAsync([FromBody] int hours)
        {
            await Task.CompletedTask;
            return new Dictionary<DateTime, IDictionary<NodeHealth, int>>();
        }
    }
}
