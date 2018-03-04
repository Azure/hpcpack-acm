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
        public string Content { get; private set; }

        public DateTime Time { get; private set; }

        public EventType Type { get; private set; }

        public EventSource Source { get; private set; }
    }
}