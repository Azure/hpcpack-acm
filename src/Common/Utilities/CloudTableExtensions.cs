namespace Microsoft.HpcAcm.Common.Utilities
{
    using Microsoft.WindowsAzure.Storage.Table;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    public static class CloudTableExtensions
    {
        // TODO use the extensions
        public static async Task<List<(string, string, T)>> QueryAsync<T>(this CloudTable t, string queryString, int? count, CancellationToken token)
        {
            var items = new List<(string, string, T)>();
            TableContinuationToken conToken = null;

            do
            {
                var queryResult = await t.ExecuteQuerySegmentedAsync(
                    new TableQuery<JsonTableEntity>().Where(queryString).Take(count),
                    conToken, null, null, token);

                items.AddRange(queryResult.Results.Select(r => (r.PartitionKey, r.RowKey, r.GetObject<T>())));

                conToken = queryResult.ContinuationToken;
            }
            while (conToken != null);

            return items;
        }


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
