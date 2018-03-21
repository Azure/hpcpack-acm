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
        private readonly CloudTable table;
        private readonly ILogger logger;

        public MetricsWorker(CloudUtilities utilities, IConfiguration config, CloudTable metricsTable, ILoggerFactory loggerFactory)
        {
            this.utilities = utilities;
            this.config = config;
            this.table = metricsTable;
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
                var result = await this.table.ExecuteQuerySegmentedAsync(q, conToken, null, null, token);
                categories.AddRange(result.Results.Select(r => (r.RowKey, r.GetObject<string>())));

                conToken = result.ContinuationToken;
            }
            while (conToken != null);

            return categories;
        }

        public override async Task<bool> DoWorkAsync(TaskItem taskItem, CancellationToken token)
        {
            try
            {
                while (true)
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
                                    RedirectStandardInput = true
                                };

                                var process = Process.Start(psi);

                                var output = await process.StandardOutput.ReadToEndAsync();
                                var error = await process.StandardError.ReadToEndAsync();

                                return (s.Item1, string.IsNullOrEmpty(error) ? output : toErrorJson(error));
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

                        var result = await table.ExecuteAsync(TableOperation.InsertOrReplace(entity), null, null, token);

                        if (!result.IsSuccessfulStatusCode())
                        {
                            break;
                        }

                        // todo: metric options
                        await Task.Delay(this.config.GetValue<int>("MetricInterval"), token);
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "DoWorkAsync error.");
                throw;
            }
        }
    }
}
