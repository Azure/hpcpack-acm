namespace Microsoft.HpcAcm.Common.Utilities
{
    using Microsoft.WindowsAzure.Storage.Queue;
    using Newtonsoft.Json;
    using System.Threading;
    using System.Threading.Tasks;


    public static class CloudQueueExtensions
    {
        public static async Task AddMessageAsync<T>(this CloudQueue queue, T obj, CancellationToken cancellationToken)
        {
            var msg = new CloudQueueMessage(JsonConvert.SerializeObject(obj));
            await queue.AddMessageAsync(msg, null, null, null, null, cancellationToken);
        }
    }
}
