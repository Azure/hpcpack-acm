namespace Microsoft.HpcAcm.Services.Dashboard
{
    using Microsoft.Extensions.Options;
    using Microsoft.HpcAcm.Services.Common;
    using Microsoft.HpcAcm.Common.Utilities;
    using Microsoft.WindowsAzure.Storage.Table;
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using T = System.Threading.Tasks;
    using Microsoft.HpcAcm.Common.Dto;
    using System.Linq;

    internal class NodeDashboardWorker : DashboardWorkerBase
    {
        public NodeDashboardWorker(IOptions<DashboardOptions> options) : base(options)
        {
        }

        public override string PartitionKey { get => this.Utilities.GetDashboardPartitionKey("node"); }
        public override string LowestKey { get => "!"; }
        public override string HighestKey { get => "~"; }

        public override async T.Task<DashboardItem> GetStatisticsAsync(string lowerKey, string higherKey, CancellationToken token)
        {
            var states = Enum.GetValues(typeof(NodeHealth)).Cast<NodeHealth>();

            lowerKey = lowerKey ?? this.LowestKey;
            DashboardItem item = new DashboardItem()
            {
                LowerKey = lowerKey,
                Statistics = states.ToDictionary(s => s.ToString(), s => 0),
            };

            higherKey = this.HighestKey;

            while (true)
            {
                var nodes = (await this.Utilities.GetNodesAsync(lowerKey, higherKey, this.options.BatchCount, token)).ToList();
                if (nodes.Count == 0) break;

                var dict = nodes.GroupBy(n => n.Health).ToDictionary(g => g.Key, g => g.Count());
                foreach (var d in dict) { item.Statistics[d.Key.ToString()] += d.Value; }

                lowerKey = nodes[nodes.Count - 1].Id;
            }

            item.NextUpdateTime = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(this.options.ActiveUpdateIntervalSeconds);

            return item;
        }
    }
}
