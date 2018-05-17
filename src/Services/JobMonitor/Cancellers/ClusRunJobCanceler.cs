namespace Microsoft.HpcAcm.Services.JobMonitor
{
    using Microsoft.HpcAcm.Common.Dto;

    public class ClusRunJobCanceler : JobCanceler
    {
        public override JobType RestrictedJobType { get => JobType.ClusRun; }
    }
}
