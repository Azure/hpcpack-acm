namespace Microsoft.HpcAcm.Services.NodeAgent
{
    using Microsoft.Extensions.Configuration;
    using Serilog;
    using Microsoft.Extensions.Options;
    using Microsoft.HpcAcm.Common.Dto;
    using Microsoft.HpcAcm.Services.Common;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using Tasks = System.Threading.Tasks;

    public class NodeCommunicator
    {
        private const string CallbackUriHeaderName = "CallbackUri";

        private readonly HttpClient client;

        private readonly ILogger logger;
        public NodeCommunicator(ILogger logger, IOptions<NodeAgentWorkerOptions> options)
        {
            this.logger = logger;
            this.Options = options.Value;
            this.client = new HttpClient();
        }

        // TODO make node communicator options.
        public NodeAgentWorkerOptions Options { get; }

        public async Tasks.Task<string> EndJobAsync(string nodeName, EndJobArg arg,
            CancellationToken token)
        {
            return await this.SendRequestAsync("endjob",
                this.GetCallbackUri(nodeName, "taskcompleted"),
                nodeName,
                arg,
                0,
                token);
        }

        public async Tasks.Task<string> StartJobAndTaskAsync(string nodeName, StartJobAndTaskArg arg,
            string userName, string password, ProcessStartInfo startInfo, string privateKey, string publicKey,
            CancellationToken token)
        {
            return await this.SendRequestAsync("startjobandtask",
                this.GetCallbackUri(nodeName, "taskcompleted"),
                nodeName,
                Tuple.Create(arg, startInfo, userName, password, privateKey, publicKey),
                0,
                token);
        }

        private async Tasks.Task<string> SendRequestInternalAsync<T>(string action, string callbackUri, string nodeName, T arg, int retryCount, CancellationToken token)
        {
            this.logger.Information("Sending out request, action {0}, callback {1}, nodeName {2}", action, callbackUri, nodeName);

            try
            {
                var uri = this.GetResoureUri(nodeName, action);
                this.logger.Information("Sending request to {0}", uri);
                using (var response = await this.client.PostAsJsonAsync(uri, arg, new Dictionary<string, string>() { { CallbackUriHeaderName, callbackUri } }, token))
                {
                    this.logger.Information("Sending out request task completed, action {0}, callback {1}, nodeName {2} response code {3}",
                        action, callbackUri, nodeName, response.StatusCode);
                    response.EnsureSuccessStatusCode();
                    return await response.Content.ReadAsStringAsync();
                }
            }
            catch (Exception e)
            {
                this.logger.Error(e, "Sending out request, action {0}, callback {1}, nodeName {2}", action, callbackUri, nodeName);
                if (this.CanRetry(e) && retryCount < this.Options.AutoResendLimit)
                {
                    await Tasks.Task.Delay(TimeSpan.FromSeconds(this.Options.ResendIntervalSeconds), token);
                    return await this.SendRequestAsync(action, callbackUri, nodeName, arg, retryCount + 1, token);
                }
                else
                {
                    throw;
                }                
            }
        }

        private async Tasks.Task<string> SendRequestAsync<T>(string action, string callbackUri, string nodeName, T arg, int retryCount, CancellationToken token)
        {
            return await this.SendRequestInternalAsync(action, callbackUri, nodeName, arg, retryCount, token).ContinueWith(t =>
            {
                this.logger.Information("Finished sending, action {0}, callback {1}, nodeName {2} retry count {3}", action, callbackUri, nodeName, retryCount);
                return t.Result;
            });
        }

        private bool CanRetry(Exception exception)
        {
            if (exception is HttpRequestException ||
                exception is WebException ||
                exception is Tasks.TaskCanceledException)
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

        private string GetCallbackUri(string nodeName, string action) => $"{this.Options.AgentUriBase}/callback/{action}";
    }
}
