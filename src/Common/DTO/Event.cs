namespace Microsoft.HpcAcm.Common.Dto
{
    using System;

    public enum EventType
    {
        Information,
        Warning,
        Alert,
    }

    public enum EventSource
    {
        Node,
        Cluster,
        Job,
        Scheduler,        
    }

    public class Event
    {
        public long Id { get => this.Time.Ticks; }
        public string Content { get; set; }

        public DateTimeOffset Time { get; set; } = DateTimeOffset.UtcNow;

        public EventType Type { get; set; }

        public EventSource Source { get; set; }
    }
}