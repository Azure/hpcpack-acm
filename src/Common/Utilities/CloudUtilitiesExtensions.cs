namespace Microsoft.HpcAcm.Common.Utilities
{
    using Microsoft.WindowsAzure.Storage.Blob;
    using Microsoft.WindowsAzure.Storage.Queue;
    using Microsoft.WindowsAzure.Storage.Table;
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    public static class CloudUtilitiesExtensions
    {
        public static async Task<CloudTable> GetOrCreateJobsTableAsync(this CloudUtilities u, CancellationToken token)
        {
            return await u.GetOrCreateTableAsync(u.Option.JobsTableName, token);
        }

        public static async Task<CloudAppendBlob> CreateOrReplaceTaskOutputBlobAsync(this CloudUtilities u, int jobId, string key, CancellationToken token)
        {
            return await u.GetOrCreateAppendBlobAsync(
                string.Format(u.Option.JobResultContainerPattern, jobId),
                key,
                token);
        }


        public static async Task<CloudQueue> GetOrCreateJobDispatchQueueAsync(this CloudUtilities u, CancellationToken token)
        {
            return await u.GetOrCreateQueueAsync(u.Option.JobDispatchQueueName, token);
        }

        public static async Task<CloudQueue> GetOrCreateNodeDispatchQueueAsync(this CloudUtilities u, string nodeName, CancellationToken token)
        {
            return await u.GetOrCreateQueueAsync(string.Format(u.Option.NodeDispatchQueuePattern, nodeName), token);
        }

        public static CloudTable GetNodesTable(this CloudUtilities u)
        {
            return u.GetTable(u.Option.NodesTableName);
        }

        public static async Task<CloudTable> GetOrCreateNodesTableAsync(this CloudUtilities u, CancellationToken token)
        {
            return await u.GetOrCreateTableAsync(u.Option.NodesTableName, token);
        }
    }
}
