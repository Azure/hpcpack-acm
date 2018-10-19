namespace Microsoft.HpcAcm.Common.Dto
{
    using System;

    public enum NodeHealth
    {
        OK,
        Warning,
        Error,
    }

    public enum NodeState
    {
        Online,
        Offline,
    }

    public class Node
    {
        public string Id { get => this.Name; }
        public string Name { get; set; }
        public NodeHealth Health { get; set; }
        public DateTimeOffset? LastHeartbeatTime { get; set; }
        public NodeState State { get; set; }
        public int RunningJobCount { get; set; }
        public int EventCount { get; set; }
        public ComputeClusterRegistrationInformation NodeRegistrationInfo { get; set; }
    }
}