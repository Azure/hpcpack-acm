namespace Microsoft.HpcAcm.Common.Dto
{
    public class ComputeClusterGpuInformation
    {
        /// <summary>
        /// nvmlDeviceGetName
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// nvmlDeviceGetUUID
        /// </summary>
        public string Uuid { get; set; }

        /// <summary>
        /// nvmlDeviceGetPciInfo
        /// string.Format("{0:X2}:{1:X2}", pci.bus, pci.device)
        /// </summary>
        public string PciBusDevice { get; set; }

        /// <summary>
        /// nvmlDeviceGetPciInfo, Pci.busId
        /// </summary>
        public string PciBusId { get; set; }

        /// <summary>
        /// nvmlDeviceGetMemoryInfo, Unit is MB
        /// </summary>
        public long TotalMemory { get; set; }

        /// <summary>
        /// nvmlDeviceGetMaxClockInfo
        /// </summary>
        public long MaxSMClock { get; set; }
    }
}
