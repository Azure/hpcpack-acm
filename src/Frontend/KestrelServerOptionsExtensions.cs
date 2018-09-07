namespace Microsoft.HpcAcm.Frontend
{
    using Microsoft.AspNetCore.Server.Kestrel.Core;
    using System.Net;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Configuration;
    using Microsoft.AspNetCore.Hosting;

    public static class KestrelServerOptionsExtensions
    {
        public static void UseHttps(this KestrelServerOptions options)
        {
            var config = options.ApplicationServices.GetRequiredService<IConfiguration>();
            var server = config.GetSection("ServerOptions");
            options.Listen(IPAddress.Any, 443, listenOptions => listenOptions.UseHttps(server["CertPath"]));
        }
    }
}
