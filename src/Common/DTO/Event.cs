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
        private const int MaxContentLength = 20480;
        public long Id { get => this.Time.Ticks; }
        public string Content { get => this.content; set => this.content = value.Length > MaxContentLength ? value.Substring(0, MaxContentLength) : value; }
        private string content;

        public DateTimeOffset Time { get; set; } = DateTimeOffset.UtcNow;

        public EventType Type { get; set; }

        public EventSource Source { get; set; }
    }
}