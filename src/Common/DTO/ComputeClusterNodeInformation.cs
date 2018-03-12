namespace Microsoft.HpcAcm.Common.Dto
{
    using System.Collections.Generic;
    using System.Linq;

    public class ComputeClusterNodeInformation
    {
        /// <summary>
        /// Default Constructor
        /// </summary>
        public ComputeClusterNodeInformation()
        {
            this.Jobs = new List<ComputeClusterJobInformation>();
        }

        /// <summary>
        /// Name of the compute node
        /// </summary>
        /// <value></value>
        public string Name { get; set; }

        /// <summary>
        /// jobs
        /// </summary>
        public List<ComputeClusterJobInformation> Jobs { get; private set; }

        public string MacAddress { get; set; }

        public ComputeNodeAvailability Availability { get; set; }

        public bool JustStarted { get; set; }
    }
}
