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
            // todo: abstract range query
            var partitionQuery = this.utilities.GetPartitionQueryString(this.utilities.NodesPartitionKey);

            var lastRegistrationKey = this.utilities.GetRegistrationKey(lastNodeName);
            var registrationEnd = this.utilities.GetMaximumRegistrationKey();

            var registrationRangeQuery = this.utilities.GetRowKeyRangeString(lastRegistrationKey, registrationEnd);

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
                    result.Results.Select(r => r.GetObject<ComputeClusterRegistrationInformation>()));

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

            var heartbeats = new Dictionary<string, (ComputeClusterNodeInformation, DateTimeOffset)>();

            do
            {
                var result = await nodes.ExecuteQuerySegmentedAsync(q, conToken, null, null, token);
                foreach (var h in result.Results.Select(r => (r.GetObject<ComputeClusterNodeInformation>(), r.Timestamp)))
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

                if (heartbeats.TryGetValue(nodeName, out (ComputeClusterNodeInformation, DateTimeOffset) n))
                {
                    if (n.Item2.AddSeconds(this.utilities.Option.MaxMissedHeartbeats * this.utilities.Option.HeartbeatIntervalSeconds) > DateTimeOffset.UtcNow)
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
        public async Task<IActionResult> GetAsync(string name, CancellationToken token)
        {
            name = name.ToLowerInvariant();
            var registrationKey = this.utilities.GetRegistrationKey(name);

            var nodes = this.utilities.GetNodesTable();
            var result = await nodes.ExecuteAsync(TableOperation.Retrieve<JsonTableEntity>(this.utilities.NodesPartitionKey, registrationKey), null, null, token);

            if (!result.IsSuccessfulStatusCode())
            {
                return new StatusCodeResult(result.HttpStatusCode);
            }

            ComputeClusterRegistrationInformation registerInfo = (result.Result as JsonTableEntity)?.GetObject<ComputeClusterRegistrationInformation>();

            var heartbeatKey = this.utilities.GetHeartbeatKey(name);
            result = await nodes.ExecuteAsync(TableOperation.Retrieve<JsonTableEntity>(this.utilities.NodesPartitionKey, heartbeatKey), null, null, token);

            if (!result.IsSuccessfulStatusCode())
            {
                return new StatusCodeResult(result.HttpStatusCode);
            }

            var entity = result.Result as JsonTableEntity;
            ComputeClusterNodeInformation nodeInfo = entity?.GetObject<ComputeClusterNodeInformation>();

            var node = new Node() { NodeRegistrationInfo = registerInfo, Name = name, };
            if (entity?.Timestamp.AddSeconds(this.utilities.Option.MaxMissedHeartbeats * this.utilities.Option.HeartbeatIntervalSeconds) > DateTimeOffset.UtcNow)
            {
                node.Health = NodeHealth.OK;
                node.RunningJobCount = nodeInfo.Jobs.Count;
                node.EventCount = 5;
            }
            else
            {
                node.Health = NodeHealth.Error;
            }

            node.State = NodeState.Online;

            var nodeDetails = new NodeDetails() { NodeInfo = node, Jobs = nodeInfo?.Jobs, };

            var metricsKey = this.utilities.GetMinuteHistoryKey();
            result = await nodes.ExecuteAsync(TableOperation.Retrieve<JsonTableEntity>(this.utilities.GetNodePartitionKey(name), metricsKey), null, null, token);

            if (!result.IsSuccessfulStatusCode())
            {
                return new StatusCodeResult(result.HttpStatusCode);
            }

            var historyEntity = result.Result as JsonTableEntity;
            nodeDetails.History = historyEntity.GetObject<MetricHistory>();

            return new OkObjectResult(nodeDetails);
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
