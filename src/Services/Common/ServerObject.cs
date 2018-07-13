namespace Microsoft.HpcAcm.Services.Common
{
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;
    using Microsoft.HpcAcm.Common.Utilities;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Text;

    public class ServerObject
    {
        public ServerObject() { }

        public ServerObject(
            ILogger logger,
            IConfiguration config,
            CloudUtilities utilities,
            IOptions<CloudOptions> cloudOptions,
            IOptions<ServerOptions> serverOptions)
        {
            this.Logger = logger;
            this.Configuration = config;
            this.Utilities = utilities;
            this.CloudOptions = cloudOptions.Value;
            this.ServerOptions = serverOptions.Value;
        }

        public void CopyFrom(ServerObject so)
        {
            this.Logger = so.Logger;
            this.Configuration = so.Configuration;
            this.Utilities = so.Utilities;
            this.CloudOptions = so.CloudOptions;
            this.ServerOptions = so.ServerOptions;
            this.Provider = so.Provider;
        }

        public ILogger Logger { get; set; }
        public IConfiguration Configuration { get; set; }
        public CloudUtilities Utilities { get; set; }
        public CloudOptions CloudOptions { get; set; }

        public ServerOptions ServerOptions { get; set; }
        public IServiceProvider Provider { get; set; }
    }
}
