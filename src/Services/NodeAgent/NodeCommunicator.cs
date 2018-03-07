namespace Microsoft.HpcAcm.Services.NodeAgent
{
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Microsoft.HpcAcm.Services.Common;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    public class NodeCommunicator
    {
        private const string CallbackUriHeaderName = "CallbackUri";

        private HttpClient client;
        private NodeCommunicatorOptions options;

        private ILogger<NodeCommunicator> logger;
        private IConfiguration Configuration;
        public NodeCommunicator(ILoggerFactory log, IConfiguration config)
        {
            this.Configuration = config;
            this.logger = log.CreateLogger<NodeCommunicator>();
            this.options = this.Configuration.GetValue<NodeCommunicatorOptions>("NodeCommunicator");
            this.client = new HttpClient();
        }

        public async Task StartJobAndTaskAsync(string nodeName, StartJobAndTaskArg arg,
            string userName, string password, ProcessStartInfo startInfo,
            CancellationToken token)
        {
            if (IsAdmin(userName, password))
            {
                startInfo.EnvironmentVariables[Constants.CcpAdminEnv] = "1";
            }

            await this.SendRequestAsync("startjobandtask",
                this.GetCallbackUri(nodeName, "taskcompleted"),
                nodeName,
                Tuple.Create(arg, startInfo, userName, password),
                0,
                token);
        }

        private bool IsAdmin(string userName, string password) => true;


        private async Task SendRequestInternalAsync<T>(string action, string callbackUri, string nodeName, T arg, int retryCount, CancellationToken token)
        {
            this.logger.LogInformation("Sending out request, action {0}, callback {1}, nodeName {2}", action, callbackUri, nodeName);

            try
            {
                var uri = this.GetResoureUri(nodeName, action);
                this.logger.LogInformation("Sending request to {0}", uri);
                using (var response = await this.client.PostAsJsonAsync(uri, arg, new Dictionary<string, string>() { { CallbackUriHeaderName, callbackUri } }, token))
                {
                    this.logger.LogInformation("Sending out request task completed, action {0}, callback {1}, nodeName {2} response code {3}",
                        action, callbackUri, nodeName, response.StatusCode);
                    response.EnsureSuccessStatusCode();
                }
            }
            catch (Exception e)
            {
                if (this.CanRetry(e) && retryCount < this.options.AutoResendLimit)
                {
                    await Task.Delay(TimeSpan.FromSeconds(this.options.ResendIntervalSeconds), token);
                    await this.SendRequestAsync(action, callbackUri, nodeName, arg, retryCount + 1, token);
                }
            }
        }

        private async Task SendRequestAsync<T>(string action, string callbackUri, string nodeName, T arg, int retryCount, CancellationToken token)
        {
            await this.SendRequestInternalAsync(action, callbackUri, nodeName, arg, retryCount, token).ContinueWith(t =>
            {
                this.logger.LogInformation("Finished sending, action {0}, callback {1}, nodeName {2} retry count {3}", action, callbackUri, nodeName, retryCount);
            });
        }

        private bool CanRetry(Exception exception)
        {
            if (exception is HttpRequestException ||
                exception is WebException ||
                exception is TaskCanceledException)
            {
                return true;
            }

            var aggregateEx = exception as AggregateException;
            if (aggregateEx != null)
            {
                aggregateEx = aggregateEx.Flatten();
                return aggregateEx.InnerExceptions.Any(e => this.CanRetry(e));
            }

            return false;
        }

        private Uri GetResoureUri(string nodeName, string action) =>
            new Uri(new Uri(this.options.NodeManagerUriBase), $"{nodeName}/{action}");

        private string GetMetricCallbackUri(string headNodeName, int port, Guid nodeGuid)
        {
            // change to a place holder for linux node to resolve the service place.
            headNodeName = "{0}";
            return string.Format("udp://{0}:{1}/api/{2}/metricreported", headNodeName, port, nodeGuid);
        }

        private string GetCallbackUri(string nodeName, string action) => new Uri(new Uri(this.options.AgentUriBase), $"/{nodeName}/{action}").AbsoluteUri;
    }
}
