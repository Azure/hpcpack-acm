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
            Task.Run(async () =>
            {
                try
                {
                    await this.BuildAsync();
                    await this.server.RunAsync(this.cts.Token);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(ex);
                    Environment.Exit(-1);
                }
            });

        }
        public void Start()
        {
            Task.Run(async () =>
            {
                try
                {
                    await this.server.RunAsync(this.cts.Token);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(ex);
                    Environment.Exit(-1);
                }
            });
        }

        public async Task BuildAsync()
        {
            try
            {
                var token = this.cts.Token;
                this.appConfigMethod?.Invoke(this.configBuilder);
                this.Configuration = this.configBuilder.Build();
                this.loggerFactoryConfigMethod?.Invoke(this.Configuration, this.LoggerFactory);
                this.Configuration[Constants.HpcHostNameEnv] = this.Configuration.GetValue<string>(Constants.HpcHostNameEnv, null) ?? Environment.MachineName.ToLowerInvariant();

                this.CloudOptions = this.cloudOptionMethod?.Invoke(this.Configuration);
                this.Utilities = new CloudUtilities(this.CloudOptions);

                this.source = await this.taskItemSourceMethod?.Invoke(this.Utilities, this.Configuration, token);
                this.worker = await this.workerMethod?.Invoke(this.Configuration, this.Utilities, this.LoggerFactory, token);
                this.configWorkerMethod?.Invoke(this.worker);

                this.server = new Server(this.source, this.worker, this.LoggerFactory, this.serverOptions);
            }
            catch (Exception ex)
            {
                this.LoggerFactory?.CreateLogger<ServerBuilder>()?.LogError(ex, $"Error happened in {nameof(BuildAsync)}");
                throw;
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
        public ServerBuilder AddTaskItemSource(Func<CloudUtilities, IConfiguration, CancellationToken, Task<TaskItemSource>> configMethod)
        {
            this.taskItemSourceMethod = configMethod;
            return this;
        }

        private Func<CloudUtilities, IConfiguration, CancellationToken, Task<TaskItemSource>> taskItemSourceMethod;
        private TaskItemSource source;

        #endregion

        #region Worker
        public ServerBuilder ConfigureWorker(Action<WorkerBase> configMethod)
        {
            this.configWorkerMethod = configMethod;
            return this;
        }

        private Action<WorkerBase> configWorkerMethod;

        #endregion

        #region Worker
        public ServerBuilder AddWorker(Func<IConfiguration, CloudUtilities, ILoggerFactory, CancellationToken, Task<WorkerBase>> configMethod)
        {
            this.workerMethod = configMethod;
            return this;
        }

        private Func<IConfiguration, CloudUtilities, ILoggerFactory, CancellationToken, Task<WorkerBase>> workerMethod;
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

        public CloudOption CloudOptions { get; set; }
        public CloudUtilities Utilities { get; set; }
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
