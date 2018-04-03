namespace Microsoft.HpcAcm.Services.Common
{
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Microsoft.HpcAcm.Common.Utilities;
    using System;
    using System.Collections.Generic;
    using System.Text;

    public class ServerObject
    {
        public void CopyFrom(ServerObject so)
        {
            this.Logger = so.Logger;
            this.Configuration = so.Configuration;
            this.Utilities = so.Utilities;
            this.CloudOptions = so.CloudOptions;
            this.ServerOptions = so.ServerOptions;
        }

        public ILogger Logger { get; set; }
        public IConfiguration Configuration { get; set; }
        public CloudUtilities Utilities { get; set; }
        public CloudOptions CloudOptions { get; set; }

        public ServerOptions ServerOptions { get; set; }
    }
}
