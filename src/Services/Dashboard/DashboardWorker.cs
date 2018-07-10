namespace Microsoft.HpcAcm.Services.Dashboard
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Microsoft.HpcAcm.Services.Common;
    using Microsoft.HpcAcm.Common.Dto;
    using System.Threading;
    using T = System.Threading.Tasks;
    using Microsoft.HpcAcm.Common.Utilities;
    using Microsoft.WindowsAzure.Storage.Table;
    using Microsoft.WindowsAzure.Storage.Queue;
    using Newtonsoft.Json;
    using System.Linq;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Options;
    using System.Diagnostics;
    using System.IO;

    internal abstract class DashboardWorkerBase : ServerObject, IWorker
    {
        protected readonly DashboardOptions options;

        protected DashboardWorkerBase(IOptions<DashboardOptions> options)
        {
            this.options = options.Value;
        }

        private CloudTable dashboardTable;

        public virtual async T.Task InitializeAsync(CancellationToken token)
        {
            this.dashboardTable = await this.Utilities.GetOrCreateDashboardTableAsync(token);
        }

        public abstract string PartitionKey { get; }
        public abstract string LowestKey { get; }
        public abstract string HighestKey { get; }

        public abstract T.Task<DashboardItem> GetStatisticsAsync(string lowerKey, string higherKey, CancellationToken token);

        public async T.Task DoWorkAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    this.Logger.Information("Started to calculate dashboard data for {0}", this.PartitionKey);
                    var partitionQuery = this.Utilities.GetPartitionQueryString(this.PartitionKey);
                    var rowQuery = this.Utilities.GetRowKeyRangeString(
                        this.Utilities.GetDashboardRowKey(this.LowestKey),
                        this.Utilities.GetDashboardRowKey(this.HighestKey),
                        true,
                        true);

                    var q = TableQuery.CombineFilters(
                        partitionQuery,
                        TableOperators.And,
                        rowQuery);

                    var result = await this.dashboardTable.QueryAsync<DashboardItem>(q, null, token);
                    DashboardItem previous = null;

                    var it = result.GetEnumerator();
                    while (previous == null || previous.HigherKey != null)
                    {
                        DashboardItem dash = null;
                        if (it.MoveNext()) dash = it.Current.Item3;

                        if (dash == null || dash.NextUpdateTime <= DateTimeOffset.UtcNow)
                        {
                            this.Logger.Information("Get statistics for key range {0} {1}", previous?.HigherKey ?? this.LowestKey, dash?.HigherKey);
                            dash = await this.GetStatisticsAsync(previous?.HigherKey ?? this.LowestKey, dash?.HigherKey, token);
                        }

                        dash.MergeWith(previous);
                        await this.dashboardTable.InsertOrReplaceAsync(this.PartitionKey, this.Utilities.GetDashboardRowKey(dash.LowerKey), dash, token);
                        previous = dash;
                    }

                    this.Logger.Information("Saving the dashboard data for {0}", this.PartitionKey);
                    await this.dashboardTable.InsertOrReplaceAsync(this.PartitionKey, this.Utilities.GetDashboardEntryKey(), previous, token);
                }
                catch (Exception ex)
                {
                    this.Logger.Error(ex, "Error occurred in Dashboard worker");
                }

                await T.Task.Delay(this.options.AggregationPeriodSeconds * 1000, token);
            }
        }
    }
}
