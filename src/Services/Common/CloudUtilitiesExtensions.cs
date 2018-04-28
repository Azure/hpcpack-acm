namespace Microsoft.HpcAcm.Services.Common
{
    using Microsoft.HpcAcm.Common.Dto;
    using Microsoft.HpcAcm.Common.Utilities;
    using Microsoft.WindowsAzure.Storage.Table;
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    public static class CloudUtilitiesExtensions
    {
        public static async Task UpdateJobAsync(this CloudUtilities u, JobType type, int jobId, Action<Job> action, CancellationToken token)
        {
            var pKey = u.GetJobPartitionKey(type, jobId, true);
            await u.UpdateJobAsync(pKey, action, token);
            pKey = u.GetJobPartitionKey(type, jobId, false);
            await u.UpdateJobAsync(pKey, action, token);
        }

        private static async Task UpdateJobAsync(this CloudUtilities u, string jobPartitionKey, Action<Job> action, CancellationToken token)
        {
            var jobRowKey = u.JobEntryKey;

            var jobTable = u.GetJobsTable();

            var job = await jobTable.RetrieveAsync<Job>(jobPartitionKey, jobRowKey, token);
            if (job != null)
            {
                action(job);
                await jobTable.InsertOrReplaceAsJsonAsync(jobPartitionKey, jobRowKey, job, token);
            }
        }
    }
}
