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
    using Microsoft.Extensions.Options;
    using Newtonsoft.Json;

    public class ManagementOperationWorker : TaskItemWorker, IWorker
    {
        private readonly TaskItemSourceOptions options;

        private ManagementClient client;

        private CloudTable managmentTable;

        private CloudTable nodesTable;

        public ManagementOperationWorker(IOptions<TaskItemSourceOptions> options, ManagementClient client) : base(options.Value)
        {
            this.options = options.Value;
            this.client = client;
        }

        public override async T.Task InitializeAsync(CancellationToken token)
        {
            Logger.Information("Initializing the ManagementOperationWorker");
            nodesTable = await this.Utilities.GetOrCreateNodesTableAsync(token);
            managmentTable = await this.Utilities.GetOrCreateManagementOperationTableAsync(token);
            var queue = await this.Utilities.GetOrCreateManagementRequestQueueAsync(token);
            this.Source = new QueueTaskItemSource(queue, this.options);
            await base.InitializeAsync(token);
        }

        public override async T.Task<bool> ProcessTaskItemAsync(TaskItem taskItem, CancellationToken token)
        {
            var message = taskItem.GetMessage<ManagementRequestMessage>();
            try
            {
                var request = await managmentTable.RetrieveAsync<ManagementRequest>(message.OperationId, Utilities.Option.ManagementRequestRowKey, token);
                //TODO: Don't create it, just get. And don't "return" response by queue if it's not created.
                var responseQueue = await this.Utilities.GetOrCreateManagementResponseQueueAsync(message.OperationId, token);
                ManagementResponse response;
                GroupWithNodes result = null;
                GroupWithNodes input;

                try
                {
                    input = JsonConvert.DeserializeObject<GroupWithNodes>(request.Arguments);
                    Group newGroup;
                    IEnumerable<string> nodes;
                    switch (request.Operation)
                    {
                        case ManagementOperation.CreateNodeGroup:
                            newGroup = await client.CreateGroupAsync(input);
                            result = new GroupWithNodes() { Id = newGroup.Id, Name = newGroup.Name, Description = newGroup.Description, Managed = newGroup.Managed };
                            break;
                        case ManagementOperation.UpdateNodeGroup:
                            newGroup = await client.UpdateGroupAsync(input);
                            result = new GroupWithNodes() { Id = newGroup.Id, Name = newGroup.Name, Description = newGroup.Description, Managed = newGroup.Managed };
                            break;
                        case ManagementOperation.DeleteNodeGroup:
                            await client.DeleteGroupAsync(input.Id);
                            break;
                        case ManagementOperation.AddNodesToGroup:
                            nodes = await client.AddNodesToGroupAsync(input.Id, input.Nodes);
                            result = new GroupWithNodes() { Id = input.Id, Nodes = nodes };
                            break;
                        case ManagementOperation.RemoveNodesFromGroup:
                            nodes = await client.RemoveNodesFromGroupAsync(input.Id, input.Nodes);
                            result = new GroupWithNodes() { Id = input.Id, Nodes = nodes };
                            break;
                        default:
                            throw new ArgumentException($"Invalid operation `{request.Operation}'!");
                    }

                    //Update node groups in table
                    int id = result == null ? input.Id : result.Id;
                    await UpdateGroupsInTable(id, request.Operation, token);
                }
                catch (Exception e)
                {
                    //"Return" Error
                    int errorCode = -1;
                    if (e is ManagementClient.ApiError)
                    {
                        errorCode = (int)((ManagementClient.ApiError)e).StatusCode;
                    }
                    response = new ManagementResponse() { OperationId = request.OperationId, Operation = request.Operation, ErrorCode = errorCode, Error = e.ToString() };
                    await managmentTable.InsertAsync(request.OperationId, Utilities.Option.ManagementResponseRowKey, response, token);
                    await responseQueue.AddMessageAsync((ManagementResponseMessage)response, token);
                    throw;
                }
                //"Return" OK
                response = new ManagementResponse() { OperationId = request.OperationId, Operation = request.Operation, ErrorCode = 0 };
                if (result != null)
                {
                    response.Result = JsonConvert.SerializeObject(result);
                }
                await managmentTable.InsertAsync(request.OperationId, Utilities.Option.ManagementResponseRowKey, response, token);
                await responseQueue.AddMessageAsync((ManagementResponseMessage)response, token);
            }
            catch (Exception e)
            {
                this.Logger.Error($"Error in ManagementOperationWorker when processing operation {message.OperationId}:\n{e}");
            }

            return true; //Return true to remove the message from queue anyway.
        }

        private async T.Task UpdateGroupsInTable(int groupId, ManagementOperation operation, CancellationToken token)
        {
            if (operation == ManagementOperation.DeleteNodeGroup)
            {
                await nodesTable.DeleteAsync(Utilities.GroupsPartitionKey, Utilities.GetGroupKey(groupId), token);
            }
            else
            {
                var group = await client.GetGroupAsync(groupId);
                var nodes = await client.GetNodesOfGroupAsync(groupId);
                var result = new GroupWithNodes() { Id = group.Id, Name = group.Name, Description = group.Description, Managed = group.Managed, Nodes = nodes };
                await nodesTable.InsertOrReplaceAsync(Utilities.GroupsPartitionKey, Utilities.GetGroupKey(group.Id), result, token);
            }
        }
    }
}
