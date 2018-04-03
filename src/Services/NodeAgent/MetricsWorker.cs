namespace Microsoft.HpcAcm.Services.NodeAgent
{
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Options;
    using Microsoft.Extensions.Logging;
    using Microsoft.HpcAcm.Common.Dto;
    using Microsoft.HpcAcm.Common.Utilities;
    using Microsoft.HpcAcm.Services.Common;
    using Microsoft.WindowsAzure.Storage.Table;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    public class MetricsWorker : ServerObject, IWorker
    {
        private CloudTable metricsTable;
        private CloudTable nodesTable;
        private readonly MetricsWorkerOptions workerOptions;

        public MetricsWorker(IOptions<MetricsWorkerOptions> options)
        {
            this.workerOptions = options.Value;
        }

        public async Task InitializeAsync(CancellationToken token)
        {
            this.metricsTable = await this.Utilities.GetOrCreateMetricsTableAsync(token);
            this.nodesTable = await this.Utilities.GetOrCreateNodesTableAsync(token);
        }

        public async Task<IList<(string, string)>> GetMetricScriptsAsync(CancellationToken token)
        {
            var partitionQuery = this.Utilities.GetPartitionQueryString(this.Utilities.MetricsCategoriesPartitionKey);

            var q = new TableQuery<JsonTableEntity>().Where(partitionQuery);

            TableContinuationToken conToken = null;

            var categories = new List<(string, string)>();

            do
            {
                var result = await this.metricsTable.ExecuteQuerySegmentedAsync(q, conToken, null, null, token);
                categories.AddRange(result.Results.Select(r => (r.RowKey, r.GetObject<string>())));

                conToken = result.ContinuationToken;
            }
            while (conToken != null);

            return categories;
        }

        public async Task DoWorkAsync(CancellationToken token)
        {
            long currentMinute = 0;
            while (true)
            {
                await Task.Delay(TimeSpan.FromSeconds(this.workerOptions.MetricsIntervalSeconds), token);

                try
                {
                    var nodeName = this.ServerOptions.HostName;

                    using (this.Logger.BeginScope("do metrics loop on {0}", nodeName))
                    {
                        // TODO: different frequency
                        IList<(string, string)> metricScripts = await this.GetMetricScriptsAsync(token);
                        string toErrorJson(string e) =>
                            JsonConvert.SerializeObject(new Dictionary<string, string>() { { "Error", e } }, Formatting.Indented);

                        var results = await Task.WhenAll(metricScripts.Select(async s =>
                        {
                            try
                            {
                                this.Logger.LogDebug("Collect metrics for {0}", s.Item1);

                                var scriptOutput = await PythonExecutor.ExecuteAsync(s.Item2);
                                return (s.Item1, string.IsNullOrEmpty(scriptOutput.Item2) ? scriptOutput.Item1 : toErrorJson(scriptOutput.Item2));
                            }
                            catch (Exception ex)
                            {
                                return (s.Item1, toErrorJson(ex.ToString()));
                            }
                        }));

                        DynamicTableEntity entity = new DynamicTableEntity(
                            this.Utilities.MetricsValuesPartitionKey,
                            nodeName,
                            "*",
                            results.ToDictionary(
                                r => r.Item1,
                                r => new EntityProperty(r.Item2)));

                        var result = await metricsTable.ExecuteAsync(TableOperation.InsertOrReplace(entity), null, null, token);

                        if (!result.IsSuccessfulStatusCode())
                        {
                            continue;
                        }

                        var nodesPartitionKey = this.Utilities.GetNodePartitionKey(nodeName);
                        var time = DateTimeOffset.UtcNow;

                        var minuteHistoryKey = this.Utilities.GetMinuteHistoryKey();

                        result = await this.nodesTable.ExecuteAsync(TableOperation.Retrieve<JsonTableEntity>(nodesPartitionKey, minuteHistoryKey), null, null, token);

                        var history = result.Result is JsonTableEntity historyEntity ? historyEntity.GetObject<MetricHistory>() : new MetricHistory(TimeSpan.FromSeconds(10));
                        var currentMetrics = results.Select(r => new MetricItem()
                        {
                            Category = r.Item1,
                            InstanceValues = JsonConvert.DeserializeObject<Dictionary<string, double?>>(r.Item2)
                        }).ToList();

                        history.Range = TimeSpan.FromSeconds(10);
                        history.Put(time, currentMetrics);

                        result = await this.nodesTable.ExecuteAsync(TableOperation.InsertOrReplace(new JsonTableEntity(nodesPartitionKey, minuteHistoryKey, history)), null, null, token);
                        if (!result.IsSuccessfulStatusCode())
                        {
                            continue;
                        }

                        var minute = time.UtcTicks / TimeSpan.TicksPerMinute;
                        if (minute > currentMinute)
                        {
                            currentMinute = minute;

                            // persist minute data
                            var currentMetricsEntity = new JsonTableEntity(this.Utilities.GetNodePartitionKey(nodeName),
                                this.Utilities.GetMinuteHistoryKey(currentMinute),
                                currentMetrics);

                            result = await this.nodesTable.ExecuteAsync(TableOperation.InsertOrReplace(currentMetricsEntity), null, null, token);
                            if (!result.IsSuccessfulStatusCode())
                            {
                                continue;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    this.Logger.LogError(ex, "DoWorkAsync error.");
                }
            }
        }
    }
}
