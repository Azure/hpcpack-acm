namespace Microsoft.HpcAcm.Services.NodeAgent
{
    using Microsoft.Extensions.Options;
    using Microsoft.HpcAcm.Common.Dto;
    using Microsoft.HpcAcm.Common.Utilities;
    using Microsoft.HpcAcm.Services.Common;
    using Microsoft.WindowsAzure.Storage.Table;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using T = System.Threading.Tasks;

    public class NodeRegister : ServerObject
    {
        public T.Task InitializeAsync(CancellationToken token)
        {
            this.Logger.Information("Initializing the NodeRegister");
            return T.Task.CompletedTask;
        }

        public async T.Task RegisterNodeAsync(ComputeClusterRegistrationInformation info, CancellationToken token)
        {
            var nodeName = info.NodeName.ToLowerInvariant();
            this.Logger.Information("RegisterRequested, NodeName {0}, Distro {1} ", nodeName, info.DistroInfo);
            var nodeTable = this.Utilities.GetNodesTable();

            var jsonTableEntity = new JsonTableEntity(this.Utilities.NodesPartitionKey, this.Utilities.GetRegistrationKey(nodeName), info);
            var result = await nodeTable.ExecuteAsync(TableOperation.InsertOrReplace(jsonTableEntity), null, null, token);

            using (HttpResponseMessage r = new HttpResponseMessage((HttpStatusCode)result.HttpStatusCode))
            {
                r.EnsureSuccessStatusCode();
            }
        }
    }
}
