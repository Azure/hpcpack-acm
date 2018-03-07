namespace Microsoft.HpcAcm.Services.Common
{
    using Microsoft.WindowsAzure.Storage.Table;
    using System;
    using System.Collections.Generic;
    using System.Text;

    public class GenericTableEntity<T> : TableEntity
    {
        public GenericTableEntity(string partitionKey, string rowKey, T t) : base(partitionKey, rowKey)
        {
            this.Data = t;
        }

        public T Data { get; set; }
    }
}
