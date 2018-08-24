namespace Microsoft.HpcAcm.Common.Utilities
{
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Serilog;

    internal class ManagedServiceIdentityClient
    {
        internal class AccessToken
        {
            public string access_token { get; set; }
            public string refresh_token { get; set; }
            public string expires_in { get; set; }
            public string expires_on { get; set; }
            public string not_before { get; set; }
            public string resource { get; set; }
            public string token_type { get; set; }
        }

        internal class ResourceGroupInfo
        {
            internal class Tags
            {
                public string StorageConfiguration { get; set; }
            }
            public Tags tags { get; set; }
        }

        internal class Compute
        {
            public string tags { get; set; }
            public string resourceGroupName { get; set; }
            public string subscriptionId { get; set; }
        }

        internal class StorageKey
        {
            public string keyName { get; set; }
            public string permissions { get; set; }
            public string value { get; set; }
        }

        internal class StorageKeys
        {
            public List<StorageKey> keys { get; set; }
        }

        private readonly CloudOptions options;
        private readonly ILogger logger;

        public ManagedServiceIdentityClient(CloudOptions options, ILogger logger)
        {
            this.options = options;
            this.logger = logger;
        }

        public async Task<StorageConfiguration> GetStorageConfigAsync(CancellationToken token)
        {
            // get token
            this.logger.Information("Getting access token");
            var accessToken = await this.GetAccessKeyAsync(token);

            // resolve config
            this.logger.Information("Getting storage config");
            var config = await this.GetStorageConfigAsync(accessToken, token);

            // get keys
            if (config == null) return null;
            this.logger.Information("Getting storage key");
            var requestUri = this.GetListStorageKeyUri(config);
            var keys = await Curl.PostAsync<StorageKeys, object>(requestUri, new Dictionary<string, string>() { { "Authorization", $"Bearer {accessToken}" } }, null, token);
            var key = keys.keys.FirstOrDefault(k => config.KeyName == null || string.Equals(k.keyName, config.KeyName, StringComparison.OrdinalIgnoreCase));
            config.KeyValue = key?.value;
            return config;
        }

        private StorageConfiguration GetConfigFromTags(string tags, bool rawConfig = false)
        {
            this.logger.Information("Getting config from tags {0}", tags);

            string rawData = null;
            if (!rawConfig)
            {
                var tagTokens = tags?.Split(';');
                var configToken = tagTokens?.FirstOrDefault(t => t.StartsWith(nameof(StorageConfiguration)));
                var tokens = configToken?.Split(new char[] { ':' }, 2);
                if (tokens?.Length == 2)
                {
                    rawData = tokens[1];
                }
            }
            else
            {
                rawData = tags;
            }

            if (rawData != null)
            {
                this.logger.Information("deserializing config {0}", rawData);
                try
                {
                    return JsonConvert.DeserializeObject<StorageConfiguration>(rawData);
                }
                catch (JsonSerializationException)
                {
                    this.logger.Information("returning null");
                    return null;
                }
            }
            else
            {
                this.logger.Information("returning null");
                return null;
            }
        }

        private async Task<StorageConfiguration> GetStorageConfigAsync(string accessToken, CancellationToken token)
        {
            // 1. Tags
            //      1.1 Node tags
            //      1.2 Resource group tags
            // 2. Config
            // 3. Node Metadata Inspections

            var compute = await Curl.GetAsync<Compute>(this.options.ArmComputeMetadataUri, new Dictionary<string, string>() { { "Metadata", "true" } }, token);
            this.logger.Information("queried metadata.");
            var config = this.GetConfigFromTags(compute.tags);

            if (config == null)
            {
                this.logger.Information("Getting tags from resource group {0}, subid {1}", compute.resourceGroupName, compute.subscriptionId);
                config = this.GetConfigFromTags((await Curl.GetAsync<ResourceGroupInfo>(
                    string.Format(this.options.ArmResourceGroupUri, compute.subscriptionId, compute.resourceGroupName),
                    new Dictionary<string, string>() { { "Authorization", $"Bearer {accessToken}" } },
                    token)).tags?.StorageConfiguration, true);
            }

            if (config == null)
            {
                this.logger.Information("Couldn't get config from tags, getting from config file");
                if (this.options.Storage == null)
                {
                    this.logger.Information("Couldn't get config from config file, returning null");
                    return null;
                }

                config = this.options.Storage.Clone();
            }

            this.logger.Information("Config Info From config file: SubscriptionId {0}, resource group {1}", config.SubscriptionId, config.ResourceGroup);
            config.SubscriptionId = config.SubscriptionId ?? compute.subscriptionId;
            config.ResourceGroup = config.ResourceGroup ?? compute.resourceGroupName;

            this.logger.Information("Config Info Combined with metadata: SubscriptionId {0}, resource group {1}", config.SubscriptionId, config.ResourceGroup);
            return config;
        }

        private async Task<string> GetAccessKeyAsync(CancellationToken token)
        {
            var accessToken = await Curl.GetAsync<AccessToken>(this.options.ArmMsiUri, new Dictionary<string, string>() { { "Metadata", "true" } }, token);
            this.logger.Information("Got access token expires in {0}, expires on {1}, not before {2}, now {3}, resource {4}, token type {5}",
                accessToken.expires_in, accessToken.expires_on, accessToken.not_before, (DateTimeOffset.Now - new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.FromSeconds(0))).TotalSeconds,
                accessToken.resource, accessToken.token_type);
            return accessToken.access_token;
        }

        private string GetListStorageKeyUri(StorageConfiguration config) =>
            string.Format(this.options.ArmListStorageKeyUri, config.SubscriptionId, config.ResourceGroup, config.AccountName);
    }
}
