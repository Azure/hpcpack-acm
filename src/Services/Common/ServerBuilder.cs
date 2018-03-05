namespace Microsoft.HpcAcm.Services.Common
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Microsoft.HpcAcm.Common.Utilities;

    public class ServerBuilder : IDisposable
    {
        private CancellationTokenSource cts = new CancellationTokenSource();

        public void BuildAndStart()
        {
            this.BuildAndStartAsync(this.cts.Token).FireAndForget();
        }

        public async Task BuildAndStartAsync(CancellationToken token)
        {
            try
            {
                this.appConfigMethod?.Invoke(this.configBuilder);
                this.Configuration = this.configBuilder.Build();
                this.loggerFactoryConfigMethod?.Invoke(this.Configuration, this.LoggerFactory);

                this.CloudOptions = this.cloudOptionMethod?.Invoke(this.Configuration);
                this.Utilities = new CloudUtilities(this.CloudOptions);

                this.source = await this.taskItemSourceMethod?.Invoke(this.Utilities, token);
                this.worker = await this.workerMethod?.Invoke(this.Utilities, this.LoggerFactory, token);

                this.server = new Server(this.source, this.worker, this.LoggerFactory, this.serverOptions);
                await this.server.RunAsync(token);
            }
            catch(Exception ex)
            {
                this.LoggerFactory?.CreateLogger<ServerBuilder>()?.LogError(ex, $"Error happened in {nameof(BuildAndStartAsync)}");
            }
        }

        private Server server;

        public void Stop()
        {
            this.cts.Cancel();
        }

        #region Server options

        public ServerOptions serverOptions { get; set; } = new ServerOptions();
        private Action<IConfiguration, ServerOptions> serverOptionsConfigMethod;

        public ServerBuilder ConfigServerOptions(Action<IConfiguration, ServerOptions> configMethod)
        {
            this.serverOptionsConfigMethod = configMethod;
            return this;
        }

        #endregion

        #region Logger factory

        public ILoggerFactory LoggerFactory { get; set; } = new LoggerFactory();
        private Action<IConfiguration, ILoggerFactory> loggerFactoryConfigMethod;

        public ServerBuilder ConfigureLogging(Action<IConfiguration, ILoggerFactory> configMethod)
        {
            this.loggerFactoryConfigMethod = configMethod;
            return this;
        }

        #endregion

        #region Task item source
        public ServerBuilder AddTaskItemSource(Func<CloudUtilities, CancellationToken, Task<TaskItemSource>> configMethod)
        {
            this.taskItemSourceMethod = configMethod;
            return this;
        }

        private Func<CloudUtilities, CancellationToken, Task<TaskItemSource>> taskItemSourceMethod;
        private TaskItemSource source;

        #endregion

        #region Worker
        public ServerBuilder AddWorker(Func<CloudUtilities, ILoggerFactory, CancellationToken, Task<WorkerBase>> configMethod)
        {
            this.workerMethod = configMethod;
            return this;
        }

        private Func<CloudUtilities, ILoggerFactory, CancellationToken, Task<WorkerBase>> workerMethod;
        private WorkerBase worker;

        #endregion

        #region App config
        public ServerBuilder ConfigureAppConfiguration(Action<IConfigurationBuilder> configMethod)
        {
            this.appConfigMethod = configMethod;
            return this;
        }

        private IConfigurationBuilder configBuilder = new ConfigurationBuilder();
        private Action<IConfigurationBuilder> appConfigMethod;
        public IConfiguration Configuration;

        #endregion

        #region cloud options
        public ServerBuilder ConfigureCloudOptions(Func<IConfiguration, CloudOption> cloudOptionMethod)
        {
            this.cloudOptionMethod = cloudOptionMethod;
            return this;
        }

        protected CloudOption CloudOptions { get; set; }
        protected CloudUtilities Utilities { get; set; }
        private Func<IConfiguration, CloudOption> cloudOptionMethod;

        #endregion

        #region Disposable

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool isDisposing)
        {
            if (isDisposing)
            {
                this.cts?.Cancel();
                this.cts?.Dispose();
                this.cts = null;
            }
        }

        #endregion
    }
}
