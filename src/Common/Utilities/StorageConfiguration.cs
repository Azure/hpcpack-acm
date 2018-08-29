namespace Microsoft.HpcAcm.Common.Utilities
{
    public class StorageConfiguration
    {
        public string SasToken { get; set; } = "?sv=2017-07-29&ss=bfqt&srt=sco&sp=rwdlacup&se=2019-04-24T18:19:28Z&st=2018-04-25T10:19:28Z&spr=https&sig=pYCVmT40eW54msV7P9F%2BMhBwPUbHr0HYGHvogafCs1I%3D";
        public string AccountName { get; set; } = "evanchpcacm";
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
