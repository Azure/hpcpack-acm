namespace Microsoft.HpcAcm.Services.NodeReporter.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.HpcAcm.Common.Dto;

    [Route("api/[controller]")]
    public class NodesController : Controller
    {
        // GET api/nodes
        [HttpGet]
        public async Task<IEnumerable<Node>> GetAsync()
        {
            await Task.CompletedTask;
            return new Node[] { new Node() };
        }

        // GET api/nodes/node1
        [HttpGet("{name}")]
        public async Task<NodeDetails> GetAsync(string name)
        {
            await Task.CompletedTask;
            return new NodeDetails();
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
