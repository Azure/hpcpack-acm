namespace Microsoft.HpcAcm.Services.Common
{
    using Microsoft.HpcAcm.Common.Dto;
    using Microsoft.HpcAcm.Common.Utilities;
    using Microsoft.WindowsAzure.Storage.Table;
    using System;
    using System.Collections.Generic;
    using System.Text;

    public class DiagnosticsJobTableEntity: TableEntity
    {
        public DiagnosticsJob Job { get; set; }

        public DiagnosticsJobTableEntity(DiagnosticsJob job, CloudUtilities utilities) : base(utilities.GetJobPartitionName(job.Id), utilities.JobEntryKey)
        {

        }
    }
}
