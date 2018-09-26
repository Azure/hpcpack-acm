namespace Microsoft.HpcAcm.Frontend
{
    using Bazinga.AspNetCore.Authentication.Basic;
    using System.Threading.Tasks;
    using Microsoft.HpcAcm.Common.Utilities;
    using Microsoft.Extensions.Configuration;

    public class BasicCredentialVerifier : IBasicCredentialVerifier
    {
        private string Username { get; }

        private string Password { get; }

        public BasicCredentialVerifier(IConfiguration config)
        {
            var server = config.GetSection("ServerOptions");
            this.Password = server["Password"];
            this.Username = server["Username"];
        }

        public Task<bool> Authenticate(string username, string password)
        {
            return Task<bool>.FromResult(Username == username && Password == password);
        }
    }
}
