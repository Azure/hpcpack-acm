namespace Microsoft.HpcAcm.Common.Dto
{
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
        public string Name { get; private set; }
        public NodeHealth Health { get; private set; }
        public NodeState State { get; private set; }
        public int RunningJobCount { get; private set; }
        public int EventCount { get; private set; }
        public int CoreCount { get; private set; }
        public int SocketCount { get; private set; }
        public int GpuCoreCount { get; private set; }
        public int MemoryMB { get; private set; }
    }
}