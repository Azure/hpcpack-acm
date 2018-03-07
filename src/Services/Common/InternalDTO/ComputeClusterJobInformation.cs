namespace Microsoft.HpcAcm.Services.Common
{
    using System.Collections.Generic;
    using System.Linq;

    public class ComputeClusterJobInformation
    {
        /// <summary>
        /// Default Constructor
        /// </summary>
        public ComputeClusterJobInformation()
        {
            this.Tasks = new List<ComputeClusterTaskInformation>();
        }

        /// <summary>
        /// Copy Constructor used to create base object from a derived class
        /// </summary>
        public ComputeClusterJobInformation(ComputeClusterJobInformation previousJobInformation)
        {
            this.JobId = previousJobInformation.JobId;
            this.Tasks = previousJobInformation.Tasks.Select(t => new ComputeClusterTaskInformation(t)).ToList();
        }

        public int JobId { get; set; }

        public List<ComputeClusterTaskInformation> Tasks { get; private set; }
    }
}
