namespace Microsoft.HpcAcm.Common.Utilities
{
    using Microsoft.WindowsAzure.Storage.Table;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Text;

    public class JsonTableEntity : TableEntity
    {
        public JsonTableEntity() { }

        public JsonTableEntity(string partitionKey, string rowKey, object obj) : base(partitionKey, rowKey)
        {
            this.JsonContent = JsonConvert.SerializeObject(obj, Formatting.Indented);
        }

        public string JsonContent { get; set; }

        public T GetObject<T>() => JsonConvert.DeserializeObject<T>(this.JsonContent);
    }
}
