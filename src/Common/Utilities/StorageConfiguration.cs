namespace Microsoft.HpcAcm.Common.Utilities
{
    public class StorageConfiguration
    {
        public string SasToken { get; set; }
        public string AccountName { get; set; }
        public string SubscriptionId { get; set; }
        public string ResourceGroup { get; set; }
        public string KeyName { get; set; }
        public string KeyValue { get; set; }
        public string ConnectionString { get; set; }

        public StorageConfiguration Clone() => new StorageConfiguration()
        {
            SasToken = this.SasToken,
            AccountName = this.AccountName,
            SubscriptionId = this.SubscriptionId,
            ResourceGroup = this.ResourceGroup,
            KeyName = this.KeyName,
            ConnectionString = this.ConnectionString,
            KeyValue = this.KeyValue,
        };
    }
}
