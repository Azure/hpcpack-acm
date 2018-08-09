namespace Microsoft.HpcAcm.Services.NodeAgent
{
    public class MetadataWorkerOptions
    {
        public int IntervalSeconds { get; set; } = 60;
        public int FailureRetryIntervalSeconds { get; set; } = 2;
        public int MaxFailureCount { get; set; } = 2;
        public string MetadataInstanceUri { get => "http://169.254.169.254/metadata/instance?api-version=2017-08-01"; }
        public string ScheduledEventsUri { get => "http://169.254.169.254/metadata/scheduledevents?api-version=2017-08-01"; }
    }
}
