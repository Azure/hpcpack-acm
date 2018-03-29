namespace Microsoft.HpcAcm.Services.NodeAgent
{
    using Microsoft.Extensions.Configuration;
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

    public class MetricsWorker : WorkerBase
    {
        private readonly CloudUtilities utilities;
        private readonly IConfiguration config;
        private readonly CloudTable metricsTable;
        private readonly CloudTable nodesTable;
        private readonly ILogger logger;

        public MetricsWorker(CloudUtilities utilities, IConfiguration config, CloudTable metricsTable, CloudTable nodesTable, ILoggerFactory loggerFactory)
        {
            this.utilities = utilities;
            this.config = config;
            this.metricsTable = metricsTable;
            this.nodesTable = nodesTable;
            this.logger = loggerFactory.CreateLogger<MetricsWorker>();
        }

        public async Task<IList<(string, string)>> GetMetricScriptsAsync(CancellationToken token)
        {
            var partitionQuery = this.utilities.GetPartitionQueryString(this.utilities.MetricsCategoriesPartitionKey);

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

        public override async Task<bool> DoWorkAsync(TaskItem taskItem, CancellationToken token)
        {
            long currentMinute = 0;
            while (true)
            {
                await Task.Delay(this.config.GetValue<int>("MetricInterval"), token);

                try
                {
                    var nodeName = this.config.GetValue<string>(Constants.HpcHostNameEnv);

                    using (this.logger.BeginScope("do metrics loop on {0}", nodeName))
                    {
                        // TODO: different frequency
                        IList<(string, string)> metricScripts = await this.GetMetricScriptsAsync(token);
                        string toErrorJson(string e) =>
                            JsonConvert.SerializeObject(new Dictionary<string, string>() { { "Error", e } }, Formatting.Indented);

                        var results = await Task.WhenAll(metricScripts.Select(async s =>
                        {
                            try
                            {
                                this.logger.LogDebug("Collect metrics for {0}", s.Item1);
                                var psi = new System.Diagnostics.ProcessStartInfo(
                                    @"python",
                                    $"-c \"{s.Item2.Replace("\"", "\\\"")}\"")
                                {
                                    UseShellExecute = false,
                                    CreateNoWindow = true,
                                    RedirectStandardOutput = true,
                                    RedirectStandardError = true,
                                    RedirectStandardInput = true,
                                };

                                using (var process = new Process() { StartInfo = psi, EnableRaisingEvents = true })
                                {
                                    try
                                    {
                                        process.Start();
                                        var output = await process.StandardOutput.ReadToEndAsync();
                                        var error = await process.StandardError.ReadToEndAsync();
                                        return (s.Item1, string.IsNullOrEmpty(error) ? output : toErrorJson(error));
                                    }
                                    finally
                                    {
                                        process.Kill();
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                return (s.Item1, toErrorJson(ex.ToString()));
                            }
                        }));

                        DynamicTableEntity entity = new DynamicTableEntity(
                            this.utilities.MetricsValuesPartitionKey,
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

                        var nodesPartitionKey = this.utilities.GetNodePartitionKey(nodeName);
                        var time = DateTimeOffset.UtcNow;

                        var minuteHistoryKey = this.utilities.GetMinuteHistoryKey();

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
                            var currentMetricsEntity = new JsonTableEntity(this.utilities.GetNodePartitionKey(nodeName),
                                this.utilities.GetMinuteHistoryKey(currentMinute),
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
                    this.logger.LogError(ex, "DoWorkAsync error.");
                }
            }
        }
    }
}
