namespace Microsoft.HpcAcm.Services.NodeAgent
{
    public class ScheduledEventsWorkerOptions
    {
        public int IntervalSeconds { get; set; } = 60;
        public string ScheduledEventsUri { get => "http://169.254.169.254/metadata/scheduledevents?api-version=2017-08-01"; }
    }
}
