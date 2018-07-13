namespace Microsoft.HpcAcm.Services.Dashboard
{
    using Microsoft.Extensions.Options;
    using Microsoft.HpcAcm.Common.Dto;
    using Microsoft.HpcAcm.Common.Utilities;
    using Microsoft.HpcAcm.Services.Common;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using T = System.Threading.Tasks;

    internal abstract class JobDashboardWorker : DashboardWorkerBase
    {
        protected JobDashboardWorker(IOptions<DashboardOptions> options) : base(options)
        {
        }

        public abstract JobType Type { get; }

        public override string PartitionKey { get => this.Utilities.GetDashboardPartitionKey(this.Type.ToString()); }
        public override string LowestKey { get => this.Utilities.GetJobPartitionKey(this.Type, 0); }
        public override string HighestKey { get => this.Utilities.GetJobPartitionKey(this.Type, int.MaxValue); }

        public override async T.Task<DashboardItem> GetStatisticsAsync(string lowerKey, string higherKey, CancellationToken token)
        {
            var states = Enum.GetValues(typeof(JobState)).Cast<JobState>();

            lowerKey = lowerKey ?? this.LowestKey;
            DashboardItem item = new DashboardItem()
            {
                LowerKey = lowerKey,
                HigherKey = higherKey,
                Statistics = states.ToDictionary(s => s.ToString(), s => 0),
            };

            higherKey = higherKey ?? this.HighestKey;

            var jobs = (await this.Utilities.GetJobsAsync(lowerKey, higherKey, this.options.BatchCount, this.Type, false, token)).ToList();

            var dict = jobs.GroupBy(n => n.State).ToDictionary(g => g.Key, g => g.Count());
            foreach (var d in dict) { item.Statistics[d.Key.ToString()] += d.Value; }

            bool isActive = false;
            if (jobs.Count == this.options.BatchCount)
            {
                item.HigherKey = this.Utilities.GetJobPartitionKey(this.Type, jobs[jobs.Count - 1].Id);
            }

            if (item.HigherKey == null)
            {
                isActive = true;
            }

            if (!isActive && (item.Statistics[JobState.Queued.ToString()]
                    + item.Statistics[JobState.Running.ToString()]
                    + item.Statistics[JobState.Canceling.ToString()]
                    + item.Statistics[JobState.Finishing.ToString()] > 0))
            {
                isActive = true;
            }

            item.NextUpdateTime = DateTimeOffset.UtcNow +
                TimeSpan.FromSeconds(isActive ? this.options.ActiveUpdateIntervalSeconds : this.options.InactiveUpdateIntervalSeconds);

            return item;
        }
    }
}
