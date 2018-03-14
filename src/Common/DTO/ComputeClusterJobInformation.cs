namespace Microsoft.HpcAcm.Common.Dto
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

        public int JobId { get; set; }

        public List<ComputeClusterTaskInformation> Tasks { get; private set; }
    }
}
