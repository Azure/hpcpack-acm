namespace Microsoft.HpcAcm.Services.NodeAgent
{
    using Microsoft.HpcAcm.Common.Dto;
    using Microsoft.HpcAcm.Services.Common;
    using Microsoft.WindowsAzure.Storage.Table;
    using System.Threading;
    using System.Threading.Tasks;

    public abstract class JobTaskProcessor : ServerObject
    {
        protected NodeCommunicator Communicator { get; }

        protected JobTaskProcessor(NodeCommunicator communicator)
        {
            this.Communicator = communicator;
        }

        public abstract Task<bool> ProcessAsync(TaskEventMessage message, CancellationToken token);
    }
}
