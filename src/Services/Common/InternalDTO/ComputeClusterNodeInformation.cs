namespace Microsoft.HpcAcm.Services.Common
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
        /// Copy Constructor used to create base object from a derived class
        /// </summary>
        public ComputeClusterNodeInformation(ComputeClusterNodeInformation previousNodeInformation)
        {
            this.Name = previousNodeInformation.Name;
            this.MacAddress = previousNodeInformation.MacAddress;
            this.Availability = previousNodeInformation.Availability;
            this.JustStarted = previousNodeInformation.JustStarted;

            this.Jobs = previousNodeInformation.Jobs.Select(j => new ComputeClusterJobInformation(j)).ToList();
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
