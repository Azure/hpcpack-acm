namespace Microsoft.HpcAcm.Services.JobMonitor
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

    internal class ScriptSynchronizer : TaskItemWorker, IWorker
    {
        private readonly TaskItemSourceOptions options;
        private readonly ScriptSynchronizerOptions syncOptions;

        public ScriptSynchronizer(IOptions<TaskItemSourceOptions> options, IOptions<ScriptSynchronizerOptions> syncOptions)
           : base(options.Value)
        {
            this.options = options.Value;
            this.syncOptions = syncOptions.Value;
        }

        private CloudTable jobsTable;
        private CloudTable metricsTable;
        private string diagUri;
        private string metricsUri;

        private readonly Dictionary<string, string> Headers = new Dictionary<string, string>() { { "User-Agent", "hpc-acm" } };

        private T.Task<TResult> GetAsync<TResult>(string uri, CancellationToken token) => Curl.GetAsync<TResult>(uri, this.Headers, token);
        public override async T.Task InitializeAsync(CancellationToken token)
        {
            var queue = await this.Utilities.GetOrCreateScriptSyncQueueAsync(token);
            this.Source = new QueueTaskItemSource(queue, this.options);

            var tree = await this.GetAsync<GitHubTree>(this.syncOptions.GitHubApiBase, token);
            var srcUrl = tree.tree.Single(n => n.path == "src").url;
            var srcTree = await this.GetAsync<GitHubTree>(srcUrl, token);

            this.diagUri = srcTree.tree.Single(n => n.path == "Diagnostics").url;
            this.metricsUri = srcTree.tree.Single(n => n.path == "Metrics").url;

            await this.ProcessTaskItemAsync(null, token);

            await base.InitializeAsync(token);
        }

        private async T.Task<bool> SyncMetricScriptsAsync(CancellationToken token)
        {
            this.metricsTable = this.Utilities.GetMetricsTable();
            var partitionKey = this.Utilities.MetricsCategoriesPartitionKey;
            var metricTree = await this.GetAsync<GitHubTree>(this.metricsUri, token);

            foreach (var metricNode in metricTree.tree.Where(n => n.path.EndsWith(".py")))
            {
                this.Logger.Information("Syncing metric {0}", metricNode.path);
                var fileName = metricNode.path;
                var metricScriptContent = await this.GetAsync<string>($"{this.syncOptions.GitHubContentBase}/src/Metrics/{fileName}", token);
                var dotPos = fileName.LastIndexOf('.');
                fileName = fileName.Substring(0, dotPos);

                await this.metricsTable.InsertOrReplaceBatchAsync(token, new JsonTableEntity(partitionKey, fileName, metricScriptContent));
            }

            return true;
        }

        private async T.Task<bool> UploadDiagTestScriptAsync(string containerName, GitHubTree.GitHubTreeNode pythonNode, CancellationToken token)
        {
            if (pythonNode.path == null || containerName == null) return true;

            this.Logger.Information("    Uploading script {0}", pythonNode.path);
            var scriptContent = await this.GetAsync<string>($"{this.syncOptions.GitHubContentBase}/src/Diagnostics/{pythonNode.path}", token);
            await this.Utilities.UploadToBlockBlobAsync(containerName, pythonNode.path, scriptContent, token);
            return true;
        }

        private async T.Task<bool> SyncDiagScriptsAsync(CancellationToken token)
        {
            this.jobsTable = this.Utilities.GetJobsTable();
            var diagTree = await this.GetAsync<GitHubTree>(this.diagUri, token);

            string scriptContainer = "diagtestscripts";
            foreach (var jsonFileNode in diagTree.tree.Where(t => t.path.EndsWith(".json")))
            {
                this.Logger.Information("Syncing diag test {0}", jsonFileNode.path);
                var tokens = jsonFileNode.path.Split('-', '.');
                if (tokens.Length < 3)
                {
                    this.Logger.Warning("Cannot infer the category and test name from the json file name {0}, skip the test", jsonFileNode.path);
                    continue;
                }

                var jsonContent = await this.GetAsync<InternalDiagnosticsTest>($"{this.syncOptions.GitHubContentBase}/src/Diagnostics/{jsonFileNode.path}", token);
                scriptContainer = jsonContent.DispatchScript.ContainerName;
                var diagTestPartitionKey = this.Utilities.GetDiagPartitionKey(tokens[0]);
                var diagTestRowKey = tokens[1];
                await this.jobsTable.InsertOrReplaceAsync(diagTestPartitionKey, diagTestRowKey, jsonContent, token);
            }

            foreach (var pythonFileNode in diagTree.tree.Where(t => t.path.EndsWith(".py")))
            {
                this.Logger.Information("Syncing diag test script {0}", pythonFileNode.path);
                await this.UploadDiagTestScriptAsync(scriptContainer, pythonFileNode, token);
            }

            return true;
        }

        public override async T.Task<bool> ProcessTaskItemAsync(TaskItem taskItem, CancellationToken token)
        {
            this.Logger.Information("Do work for Script sync message {0}", taskItem?.Id);

            try
            {
                var message = taskItem?.GetMessage<ScriptSyncMessage>();
                var results = await T.Task.WhenAll(this.SyncDiagScriptsAsync(token), this.SyncMetricScriptsAsync(token));
                return results.All(r => r);
            }
            catch (Exception ex)
            {
                this.Logger.Error("Exception occurred when process script sync message {0}, {1}", taskItem?.Id, ex);
            }

            return true;
        }
    }
}
