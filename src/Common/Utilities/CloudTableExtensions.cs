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
        public static async Task<IEnumerable<(string, string, T, DateTimeOffset)>> QueryAsync<T>(this CloudTable t, string queryString, int? count, CancellationToken token)
        {
            var items = new List<(string, string, T, DateTimeOffset)>();

            var entities = await t.QueryAsync(new TableQuery<JsonTableEntity>().Where(queryString), count, token);

            return entities.Select(r => (r.PartitionKey, r.RowKey, r.GetObject<T>(), r.Timestamp));
        }

        public static async Task<List<T>> QueryAsync<T>(this CloudTable t, TableQuery<T> query, int? count, CancellationToken token) where T : ITableEntity, new()
        {
            var items = new List<T>();

            if (count.HasValue && count.Value <= 0) { return items; }
            TableContinuationToken conToken = null;

            do
            {
                var queryResult = await t.ExecuteQuerySegmentedAsync(
                    query.Take(count.HasValue ? count.Value - items.Count : count),
                    conToken, null, null, token);

                items.AddRange(queryResult.Results);

                conToken = queryResult.ContinuationToken;
            }
            while (conToken != null && (count.HasValue ? items.Count < count.Value : true));

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
        public static async Task<bool> InsertOrReplaceAsJsonStringAsync(this CloudTable t, string partition, string key, string jsonString, CancellationToken token)
        {
            var entity = new JsonTableEntity(partition, key, jsonString);
            var result = await t.ExecuteAsync(TableOperation.InsertOrReplace(entity), null, null, token);
            return result.IsSuccessfulStatusCode();
        }
        public static async Task<bool> InsertOrReplaceAsJsonAsync(this CloudTable t, string partition, string key, object obj, CancellationToken token)
        {
            var entity = new JsonTableEntity(partition, key, obj);
            var result = await t.ExecuteAsync(TableOperation.InsertOrReplace(entity), null, null, token);
            return result.IsSuccessfulStatusCode();
        }
    }
}
