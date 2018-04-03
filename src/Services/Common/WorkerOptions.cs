namespace Microsoft.HpcAcm.Services.Common
{
    public class WorkerOptions
    {
        public int FetchIntervalSeconds { get; set; } = 3;
        public int FetchIntervalOnErrorSeconds { get; set; } = 10;
    }
}
