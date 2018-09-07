namespace Microsoft.HpcAcm.Services.Common
{
    using System;

    public class ServerOptions
    {
        public string HostName { get; set; } = Environment.MachineName.ToLowerInvariant();

        public string CertPath { get; set; }

        public string Password { get; set; }
    }
}
