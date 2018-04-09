namespace Microsoft.HpcAcm.Common.Utilities
{
    using Microsoft.WindowsAzure.Storage.Table;
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    public static class CloudTableExtensions
    {

        public static async Task<T> RetrieveAsync<T>(this CloudTable t, string partition, string key, CancellationToken token)
        {
            var result = await t.ExecuteAsync(TableOperation.Retrieve<JsonTableEntity>(partition, key), null, null, token);
            if (result.Result is JsonTableEntity entity)
            {
                return entity.GetObject<T>();
            }
            else
            {
                return default(T);
            }
        }

        public static async Task<JsonTableEntity> RetrieveAsJsonAsync(this CloudTable t, string partition, string key, CancellationToken token)
        {
            var result = await t.ExecuteAsync(TableOperation.Retrieve<JsonTableEntity>(partition, key), null, null, token);
            return result.Result as JsonTableEntity;
        }
        public static async Task<bool> InsertOrReplaceAsJsonAsync(this CloudTable t, string partition, string key, object obj, CancellationToken token)
        {
            var entity = new JsonTableEntity(partition, key, obj);
            var result = await t.ExecuteAsync(TableOperation.InsertOrReplace(entity), null, null, token);
            return result.IsSuccessfulStatusCode();
        }
    }
}
