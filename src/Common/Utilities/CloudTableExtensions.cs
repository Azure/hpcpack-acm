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
        public static async Task<IEnumerable<(string, string, T, DateTimeOffset)>> QueryAsync<T>(this CloudTable t, string queryString, int? count, CancellationToken token)
        {
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
            while (conToken != null && (!count.HasValue || items.Count < count.Value));

            return items;
        }

        public static Task<T> RetrieveAsync<T>(this CloudTable t, string partition, string key, CancellationToken token)
        {
            return t.RetrieveAsync<T>(partition, key, null, token);
        }

        public static async Task<T> RetrieveAsync<T>(this CloudTable t, string partition, string key, Action<JsonTableEntity, T> resolver, CancellationToken token)
        {
            var result = await t.ExecuteAsync(TableOperation.Retrieve<JsonTableEntity>(partition, key), null, null, token);
            if (result.Result is JsonTableEntity entity)
            {
                var obj = entity.GetObject<T>();
                resolver?.Invoke(entity, obj);
                return obj;
            }
            else
            {
                return default(T);
            }
        }

        public static async Task<JsonTableEntity> RetrieveJsonTableEntityAsync(this CloudTable t, string partition, string key, CancellationToken token)
        {
            var result = await t.ExecuteAsync(TableOperation.Retrieve<JsonTableEntity>(partition, key), null, null, token);
            return result.Result as JsonTableEntity;
        }
        public static async Task<TableResult> InsertAsync<T>(this CloudTable t, string partition, string key, T obj, CancellationToken token)
        {
            var entity = new JsonTableEntity(partition, key, obj);
            return await t.InsertAsync(entity, token);
        }
        public static async Task DeleteAsync(this CloudTable t, string partition, string key, CancellationToken token)
        {
            var result = await t.ExecuteAsync(TableOperation.Retrieve<JsonTableEntity>(partition, key), null, null, token);
            if (result.Result is JsonTableEntity entity)
            {
                await t.ExecuteAsync(TableOperation.Delete(entity), null, null, token);
            }
        }
        public static async Task DeleteAsync(this CloudTable t, JsonTableEntity entity, CancellationToken token)
        {
            await t.ExecuteAsync(TableOperation.Delete(entity), null, null, token);
        }
        public static async Task<TableResult> InsertOrReplaceAsync<T>(this CloudTable t, string partition, string key, T obj, CancellationToken token)
        {
            var entity = new JsonTableEntity(partition, key, obj);
            return await t.InsertOrReplaceAsync(entity, token);
        }
        public static async Task<TableResult> InsertOrReplaceAsync(this CloudTable t, JsonTableEntity entity, CancellationToken token)
        {
            return await t.ExecuteAsync(TableOperation.InsertOrReplace(entity), null, null, token);
        }
        public static async Task<TableResult> InsertAsync(this CloudTable t, JsonTableEntity entity, CancellationToken token)
        {
            return await t.ExecuteAsync(TableOperation.Insert(entity), null, null, token);
        }
        public static async Task<TableResult> ReplaceAsync(this CloudTable t, JsonTableEntity entity, CancellationToken token)
        {
            return await t.ExecuteAsync(TableOperation.Replace(entity), null, null, token);
        }
        public static async Task InsertOrReplaceBatchAsync(this CloudTable t, CancellationToken token, params JsonTableEntity[] entities)
        {
            async Task SubmitBatch(TableBatchOperation batchOperation)
            {
                if (batchOperation.Count > 0)
                {
                    var tableResults = await t.ExecuteBatchAsync(batchOperation, null, null, token);
                    if (!tableResults.All(r => r.IsSuccessfulStatusCode()))
                    {
                        throw new InvalidOperationException("Not all tasks dispatched successfully");
                    }

                    batchOperation.Clear();
                }
            }

            const int MaxBatchSize = 100;
            var batches = entities
                .Zip(Enumerable.Range(0, entities.Length), (en, i) => new { Entity = en, Index = i })
                .GroupBy(en => en.Index / MaxBatchSize)
                .Select(g =>
                {
                    var batch = new TableBatchOperation();
                    foreach (var en in g)
                    {
                        batch.InsertOrReplace(en.Entity);
                    }

                    return batch;
                });

            await Task.WhenAll(batches.Select(b => SubmitBatch(b)));
        }
    }
}
