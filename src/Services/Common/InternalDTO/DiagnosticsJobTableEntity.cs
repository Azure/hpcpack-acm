namespace Microsoft.HpcAcm.Services.Common
{
    using Microsoft.HpcAcm.Common.Dto;
    using Microsoft.HpcAcm.Common.Utilities;
    using Microsoft.WindowsAzure.Storage.Table;
    using System;
    using System.Collections.Generic;
    using System.Text;

    public class JobTableEntity: TableEntity
    {
        public Job Job { get; set; }

        public JobTableEntity(Job job, CloudUtilities utilities) : base(utilities.GetJobPartitionName(job.Id, $"{job.Type}"), utilities.JobEntryKey)
        {
            this.Job = job;
        }
    }
}
