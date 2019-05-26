namespace Microsoft.HpcAcm.Services.NodeAgent
{
    using Microsoft.HpcAcm.Services.Common;
    using Microsoft.HpcAcm.Common.Dto;
    using Microsoft.HpcAcm.Common.Utilities;
    using Microsoft.WindowsAzure.Storage.Table;
    using System;
    using System.Threading;
    using System.Collections.Generic;
    using T = System.Threading.Tasks;
    using System.Linq;

    public class ManagementSyncWorker : ServerObject, IWorker
    {
        const int SyncInterval = 30; //in second

        private ManagementClient client;

        private CloudTable nodesTable;

        public ManagementSyncWorker(ManagementClient client)
        {
            this.client = client;
        }

        public async T.Task InitializeAsync(CancellationToken token)
        {
            Logger.Information("Initializing the ManagementSyncWorker");
            nodesTable = await this.Utilities.GetOrCreateNodesTableAsync(token);
        }

        public async T.Task DoWorkAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await Sync(token);
                }
                catch (Exception ex)
                {
                    this.Logger.Error(ex, "ManagementSyncWorker exception");
                }

                await T.Task.Delay(TimeSpan.FromSeconds(SyncInterval), token);
            }
        }

        private async T.Task Sync(CancellationToken token)
        {
            var oldGroups = await Utilities.GetNodeGroupsAsync(token);
            var groups = await client.GetGroupsAsync();
            //Create/Update groups
            foreach (var group in groups)
            {
                var nodes = await client.GetNodesOfGroupAsync(group.Id);
                var newGroup = new GroupWithNodes() { Id = group.Id, Name = group.Name, Description = group.Description, Managed = group.Managed, Nodes = nodes };
                await nodesTable.InsertOrReplaceAsync(Utilities.GroupsPartitionKey, Utilities.GetGroupKey(group.Id), newGroup, token);
            }
            //Delete groups
            var groupSet = new HashSet<int>(groups.Select(g => g.Id));
            foreach (var group in oldGroups)
            {
                if (!groupSet.Contains(group.Id))
                {
                    await nodesTable.DeleteAsync(Utilities.GroupsPartitionKey, Utilities.GetGroupKey(group.Id), token);
                }
            }
        }
    }
}
