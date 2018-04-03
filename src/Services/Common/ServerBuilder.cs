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
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Microsoft.HpcAcm.Common.Utilities;

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

                this.LoggerFactory = new LoggerFactory()
                    .AddConsole(configuration.GetSection("Logging").GetSection("Console"))
                    .AddDebug(LogLevel.Debug);

                this.loggerFactoryConfigMethod?.Invoke(configuration, this.LoggerFactory);

                this.logger = this.LoggerFactory.CreateLogger<ServerBuilder>();
                this.services.AddSingleton(this.LoggerFactory).AddLogging();

                this.services.AddOptions();
                this.services.Configure<CloudOptions>(configuration.GetSection("CloudOptions"));
                this.services.Configure<ServerOptions>(configuration.GetSection("ServerOptions"));

                this.services.AddSingleton<CloudUtilities>();

                this.serviceCollectionConfigMethod?.Invoke(this.services, configuration, token);
                this.services.AddSingleton<Server>();

                var provider = this.services.BuildServiceProvider();

                var server = provider.GetService<Server>();
                server.Workers.ForEach(w =>
                {
                    if (w is ServerObject so)
                    {
                        so.Configuration = configuration;
                        so.Logger = this.LoggerFactory.CreateLogger(w.GetType());
                        so.CloudOptions = provider.GetService<IOptions<CloudOptions>>().Value;
                        so.Utilities = provider.GetService<CloudUtilities>();
                        so.ServerOptions = provider.GetService<IOptions<ServerOptions>>().Value;
                    }

                    w.InitializeAsync(token).Wait();
                });

                this.Utilities = provider.GetRequiredService<CloudUtilities>();
                await this.Utilities.InitializeAsync(token);

                return server;
            }
            catch (Exception ex)
            {
                this.logger?.LogError(ex, $"Error happened in {nameof(BuildAsync)}");
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

        #endregion

        #region Logger factory

        public ILoggerFactory LoggerFactory { get; private set; }
        private Action<IConfiguration, ILoggerFactory> loggerFactoryConfigMethod;

        public ServerBuilder ConfigureLogging(Action<IConfiguration, ILoggerFactory> configMethod)
        {
            this.loggerFactoryConfigMethod = configMethod;
            return this;
        }

        private ILogger<ServerBuilder> logger;

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
