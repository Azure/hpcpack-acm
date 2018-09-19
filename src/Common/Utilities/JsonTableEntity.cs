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
            this.PutObject(obj);
        }

        public JsonTableEntity(string partitionKey, string rowKey, string jsonString) : base(partitionKey, rowKey)
        {
            this.JsonContent = jsonString;
        }

        public string JsonContent { get; set; }

        public void PutObject(object obj) => this.JsonContent = JsonConvert.SerializeObject(obj, Formatting.Indented);

        public T GetObject<T>() =>
            typeof(T) == typeof(string) && !(this.JsonContent.StartsWith(@"""") && this.JsonContent.EndsWith(@"""")) ? (dynamic)this.JsonContent : JsonConvert.DeserializeObject<T>(this.JsonContent);
    }
}
