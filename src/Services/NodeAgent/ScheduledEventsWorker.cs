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

    public class ScheduledEventsWorker : ServerObject, IWorker
    {
        private CloudTable nodesTable;
        private readonly ScheduledEventsWorkerOptions workerOptions;

        public ScheduledEventsWorker(IOptions<ScheduledEventsWorkerOptions> options)
        {
            this.workerOptions = options.Value;
        }

        public async T.Task InitializeAsync(CancellationToken token)
        {
            this.nodesTable = await this.Utilities.GetOrCreateNodesTableAsync(token);
        }

        public async T.Task DoWorkAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var nodeName = this.ServerOptions.HostName;
                    string content = null;
                    using (HttpClient c = new HttpClient())
                    {
                        HttpRequestMessage msg = new HttpRequestMessage(HttpMethod.Get, this.workerOptions.ScheduledEventsUri);
                        msg.Headers.Add("Metadata", "true");
                        var response = await c.SendAsync(msg, token);
                        response.EnsureSuccessStatusCode();
                        content = await response.Content.ReadAsStringAsync();
                    }

                    var nodesPartitionKey = this.Utilities.GetNodePartitionKey(nodeName);
                    var scheduledEventsKey = this.Utilities.GetScheduledEventsKey();

                    var obj = JsonConvert.DeserializeObject(content);
                    await this.nodesTable.InsertOrReplaceAsJsonAsync(nodesPartitionKey, scheduledEventsKey, obj, token);
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
