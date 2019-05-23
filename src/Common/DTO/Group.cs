namespace Microsoft.HpcAcm.Common.Dto
{
    using System.Collections.Generic;

    public class Group
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public bool Managed { get; set; }
    }

    public class GroupWithNodes : Group
    {
        public IEnumerable<string> Nodes { get; set; }
    }
}
