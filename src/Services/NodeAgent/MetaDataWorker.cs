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
    using T = System.Threading.Tasks;
    using System.IO;
    using System.Net.Http;

    public class MetadataWorker : ServerObject, IWorker
    {
        private CloudTable nodesTable;
        private readonly MetadataWorkerOptions workerOptions;

        public MetadataWorker(IOptions<MetadataWorkerOptions> options)
        {
            this.workerOptions = options.Value;
        }

        public async T.Task InitializeAsync(CancellationToken token)
        {
            this.nodesTable = await this.Utilities.GetOrCreateNodesTableAsync(token);
        }

        public async T.Task DoWorkAsync(CancellationToken token)
        {
            await T.Task.WhenAll(
                this.DoWorkAsync(this.workerOptions.MetadataInstanceUri, this.Utilities.GetMetadataKey(), token),
                this.DoWorkAsync(this.workerOptions.ScheduledEventsUri, this.Utilities.GetScheduledEventsKey(), token));
        }

        public async T.Task DoWorkAsync(string uri, string storageKey, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var nodeName = this.ServerOptions.HostName;
                    string content = null;
                    using (HttpClient c = new HttpClient())
                    {
                        HttpRequestMessage msg = new HttpRequestMessage(HttpMethod.Get, uri);
                        msg.Headers.Add("Metadata", "true");
                        var response = await c.SendAsync(msg, token);
                        response.EnsureSuccessStatusCode();
                        content = await response.Content.ReadAsStringAsync();
                    }

                    var nodesPartitionKey = this.Utilities.GetNodePartitionKey(nodeName);

                    await this.nodesTable.InsertOrReplaceAsync(nodesPartitionKey, storageKey, content, token);
                }
                catch (Exception ex)
                {
                    this.Logger.LogError(ex, "DoWorkAsync error.");
                }

                await T.Task.Delay(TimeSpan.FromSeconds(this.workerOptions.IntervalSeconds), token);
            }
        }
    }
}
