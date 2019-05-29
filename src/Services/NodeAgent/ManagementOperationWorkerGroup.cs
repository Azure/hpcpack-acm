namespace Microsoft.HpcAcm.Services.NodeAgent
{
    using Microsoft.Extensions.Options;
    using Microsoft.HpcAcm.Services.Common;


    public class ManagementOperationWorkerGroup : WorkerGroup<ManagementOperationWorker>
    {
        public ManagementOperationWorkerGroup(IOptions<ManagementOperationWorkerGroupOptions> options) : base(options) { }
    }
}
