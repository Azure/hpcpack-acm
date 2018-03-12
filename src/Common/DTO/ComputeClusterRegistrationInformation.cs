namespace Microsoft.HpcAcm.Common.Dto
{
    public class ComputeClusterRegistrationInformation
    {
        public ComputeClusterRegistrationInformation(string nodeName, int coreCount, int socketCount, ulong memoryMegabytes)
        {
            this.NodeName = nodeName;
            this.CoreCount = coreCount;
            this.SocketCount = socketCount;
            this.MemoryMegabytes = memoryMegabytes;
        }

        public string NodeName { get; private set; }
        public int CoreCount { get; private set; }
        public int SocketCount { get; private set; }
        public ulong MemoryMegabytes { get; private set; }
        public string DistroInfo { get; set; }
        public ComputeClusterNetworkInformation[] NetworksInfo { get; set; }
        public ComputeClusterGpuInformation[] GpuInfo { get; set; }
    }
}
