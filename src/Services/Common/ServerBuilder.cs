namespace Microsoft.HpcAcm.Services.Common
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;
    using Microsoft.HpcAcm.Common.Utilities;
    using Serilog;
    using Serilog.Sinks.RollingFile;
    using Serilog.Sinks.SystemConsole;

    public class ServerBuilder : IDisposable
    {
        #region Constructor

        private readonly string[] args;

        public ServerBuilder(string[] args)
        {
            this.args = args;
        }

        #endregion

        #region Shared Properties

        public CloudUtilities Utilities { get; private set; }

        #endregion

        #region App config

        public ServerBuilder ConfigureAppConfiguration(Action<IConfigurationBuilder> configMethod)
        {
            this.appConfigMethod = configMethod;
            return this;
        }

        private Action<IConfigurationBuilder> appConfigMethod;

        #endregion

        #region Build

        private CancellationTokenSource cts = new CancellationTokenSource();

        public CancellationToken CancelToken { get => this.cts.Token; }

        public async Task<Server> BuildAsync()
        {
            try
            {
                var token = this.cts.Token;
                var dict = new Dictionary<string, string>()
                {
                    { Constants.HpcHostNameEnv, Environment.MachineName.ToLowerInvariant() }
                };

                var configBuilder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddInMemoryCollection(dict)
                    .AddJsonFile("appsettings.json", false, true)
                    .AddEnvironmentVariables()
                    .AddCommandLine(this.args);

                this.appConfigMethod?.Invoke(configBuilder);

                var configuration = configBuilder.Build();

                this.services.AddSingleton<IConfiguration>(configuration);
                this.services.AddOptions();
                this.services.Configure<CloudOptions>(configuration.GetSection("CloudOptions"));
                this.services.Configure<ServerOptions>(configuration.GetSection("ServerOptions"));
                this.services.Configure<LogOptions>(configuration.GetSection("LogOptions"));

                var logOptions = configuration.GetSection("LogOptions").Get<LogOptions>() ?? new LogOptions();
                this.LoggerConfig = new LoggerConfiguration()
                    .WriteTo.Console()
                    .WriteTo.RollingFile(logOptions.FileName);

                this.loggerConfigMethod?.Invoke(configuration, this.LoggerConfig);

                this.Logger = this.LoggerConfig.CreateLogger();

                this.services.AddSingleton(this.LoggerConfig);
                this.services.AddSingleton(this.Logger);

                this.services.AddSingleton<CloudUtilities>();

                this.serviceCollectionConfigMethod?.Invoke(this.services, configuration, token);
                this.services.AddSingleton<Server>();
                this.services.AddSingleton<ServerObject>();

                this.Provider = this.services.BuildServiceProvider();

                this.Utilities = this.Provider.GetRequiredService<CloudUtilities>();
                await this.Utilities.InitializeAsync(token);
                var serverObjectTemplate = this.Provider.GetRequiredService<ServerObject>();
                serverObjectTemplate.Provider = this.Provider;

                var server = this.Provider.GetService<Server>();
                server.CopyFrom(serverObjectTemplate);
                await server.InitializeAsync(token);

                return server;
            }
            catch (Exception ex)
            {
                this.Logger?.Error(ex, $"Error happened in {nameof(BuildAsync)}");
                throw;
            }
        }

        public void Stop()
        {
            this.cts?.Cancel();
        }

        #endregion

        #region Service collection

        private readonly ServiceCollection services = new ServiceCollection();

        private Action<ServiceCollection, IConfiguration, CancellationToken> serviceCollectionConfigMethod;

        public ServerBuilder ConfigServiceCollection(Action<ServiceCollection, IConfiguration, CancellationToken> configMethod)
        {
            this.serviceCollectionConfigMethod = configMethod;
            return this;
        }

        public ServiceProvider Provider { get; private set; }

        public T GetRequiredService<T>()
        {
            // language 7.0 constraint.
            var svc = this.Provider.GetRequiredService(typeof(T));
            if (svc is ServerObject so)
            {
                so.CopyFrom(this.Provider.GetRequiredService<ServerObject>());
            }

            return (T)svc;
        }

        #endregion

        #region Serilog

        public LoggerConfiguration LoggerConfig { get; private set; }

        private Action<IConfiguration, LoggerConfiguration> loggerConfigMethod;

        public ServerBuilder ConfigureLogging(Action<IConfiguration, LoggerConfiguration> configMethod)
        {
            this.loggerConfigMethod = configMethod;
            return this;
        }

        public ILogger Logger { get; private set; }

        #endregion

        #region Disposable

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool isDisposing)
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
