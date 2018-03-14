namespace Microsoft.HpcAcm.Services.NodeAgent
{
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Microsoft.HpcAcm.Common.Dto;
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

        private readonly HttpClient client;

        private readonly ILogger<NodeCommunicator> logger;
        private IConfiguration Configuration { get; }
        public NodeCommunicator(ILoggerFactory log, IConfiguration config)
        {
            this.Configuration = config;
            this.logger = log.CreateLogger<NodeCommunicator>();
            this.Options = this.Configuration.GetSection("NodeCommunicator").Get<NodeCommunicatorOptions>();
            this.client = new HttpClient();
        }

        public NodeCommunicatorOptions Options { get; }

        public async Task StartJobAndTaskAsync(string nodeName, StartJobAndTaskArg arg,
            string userName, string password, ProcessStartInfo startInfo,
            CancellationToken token)
        {
            if (IsAdmin(userName, password))
            {
                startInfo.environmentVariables[Constants.CcpAdminEnv] = "1";
            }

            await this.SendRequestAsync("startjobandtask",
                this.GetCallbackUri(nodeName, "taskcompleted"),
                nodeName,
                Tuple.Create(arg, startInfo, userName, password, "", ""),
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
                this.logger.LogError(e, "Sending out request, action {0}, callback {1}, nodeName {2}", action, callbackUri, nodeName);
                if (this.CanRetry(e) && retryCount < this.Options.AutoResendLimit)
                {
                    await Task.Delay(TimeSpan.FromSeconds(this.Options.ResendIntervalSeconds), token);
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

        private Uri GetResoureUri(string nodeName, string action) => new Uri($"{this.Options.NodeManagerUriBase}/{nodeName}/{action}");

        private string GetMetricCallbackUri(string headNodeName, int port, Guid nodeGuid)
        {
            // change to a place holder for linux node to resolve the service place.
            headNodeName = "{0}";
            return string.Format("udp://{0}:{1}/api/{2}/metricreported", headNodeName, port, nodeGuid);
        }

        private string GetCallbackUri(string nodeName, string action) => $"{this.Options.AgentUriBase}/callback/{action}";
    }
}
