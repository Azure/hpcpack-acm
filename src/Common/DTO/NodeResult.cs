namespace Microsoft.HpcAcm.Common.Dto
{
    using System.Collections.Generic;

    public class NodeResult
    {
        public int JobId { get; set; }

        public string NodeName { get; set; }

        public IList<CommandResult> Results { get; set; }
    }
}