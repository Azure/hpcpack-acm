namespace Microsoft.HpcAcm.Services.Common
{
    public class ServerOptions
    {
        public int FetchIntervalSeconds { get; set; } = 3;
        public int FetchIntervalOnErrorSeconds { get; set; } = 10;
    }
}
