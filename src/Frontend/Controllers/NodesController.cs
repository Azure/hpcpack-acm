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
        private readonly CloudUtilities utilities;
        private readonly ILogger logger;

        public NodesController(CloudUtilities u, ILogger<NodesController> logger)
        {
            this.utilities = u;
            this.logger = logger;
        }

        // GET api/nodes?count=50&lastNodeName=testnode
        [HttpGet()]
        public async Task<IEnumerable<Node>> GetAsync(
            [FromQuery] string lastNodeName,
            [FromQuery] int? count,
            CancellationToken token)
        {
            var partitionQuery = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, this.utilities.NodesPartitionKey);

            var lastRegistrationKey = this.utilities.GetRegistrationKey(lastNodeName);
            var registrationEnd = this.utilities.GetRegistrationKey(new string(Char.MaxValue, 1));

            var registrationRangeQuery = TableQuery.CombineFilters(
                TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.GreaterThan, lastRegistrationKey),
                TableOperators.And,
                TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.LessThan, registrationEnd));

            var q = new TableQuery<JsonTableEntity>()
                .Where(TableQuery.CombineFilters(
                    partitionQuery,
                    TableOperators.And,
                    registrationRangeQuery))
                .Take(count);

            var nodes = this.utilities.GetNodesTable();

            List<ComputeClusterRegistrationInformation> registrations = new List<ComputeClusterRegistrationInformation>();
            TableContinuationToken conToken = null;

            do
            {
                var result = await nodes.ExecuteQuerySegmentedAsync(q, conToken, null, null, token);
                registrations.AddRange(
                    result.Results.Select(r => JsonConvert.DeserializeObject<ComputeClusterRegistrationInformation>(r.JsonContent)));

                conToken = result.ContinuationToken;
            }
            while (conToken != null);

            if (!registrations.Any())
            {
                return new Node[0];
            }

            var firstHeartbeat = this.utilities.GetHeartbeatKey(registrations.First().NodeName.ToLowerInvariant());
            var lastHeartbeat = this.utilities.GetHeartbeatKey(registrations.Last().NodeName.ToLowerInvariant());

            var heartbeatRangeQuery = TableQuery.CombineFilters(
                TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.GreaterThanOrEqual, firstHeartbeat),
                TableOperators.And,
                TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.LessThanOrEqual, lastHeartbeat));

            q = new TableQuery<JsonTableEntity>()
                .Where(TableQuery.CombineFilters(
                    partitionQuery,
                    TableOperators.And,
                    heartbeatRangeQuery));

            conToken = null;

            var heartbeats = new Dictionary<string, (ComputeClusterNodeInformation, DateTime)>();

            do
            {
                var result = await nodes.ExecuteQuerySegmentedAsync(q, conToken, null, null, token);
                foreach (var h in result.Results.Select(r => JsonConvert.DeserializeObject<(ComputeClusterNodeInformation, DateTime)>(r.JsonContent)))
                {
                    heartbeats[h.Item1.Name.ToLowerInvariant()] = h;
                }

                conToken = result.ContinuationToken;
            }
            while (conToken != null);

            return registrations.Select(r =>
            {
                var nodeName = r.NodeName.ToLowerInvariant();
                var node = new Node() { NodeRegistrationInfo = r, Name = nodeName, };

                if (heartbeats.TryGetValue(nodeName, out (ComputeClusterNodeInformation, DateTime) n))
                {
                    if (n.Item2.AddSeconds(this.utilities.Option.MaxMissedHeartbeats * this.utilities.Option.HeartbeatIntervalSeconds) > DateTime.UtcNow)
                    {
                        node.Health = NodeHealth.OK;
                        node.State = NodeState.Online;
                        node.RunningJobCount = n.Item1.Jobs.Count;
                        // TODO: adding events
                        node.EventCount = 5;
                    }
                    else
                    {
                        node.Health = NodeHealth.Error;
                    }
                }

                return node;
            });
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
