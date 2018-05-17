namespace Microsoft.HpcAcm.Services.JobMonitor
{
    using Microsoft.HpcAcm.Common.Dto;

    public class DiagnosticsJobCanceler : JobCanceler
    {
        public override JobType RestrictedJobType { get => JobType.Diagnostics; }
    }
}
