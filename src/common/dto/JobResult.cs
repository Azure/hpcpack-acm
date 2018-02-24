namespace Microsoft.HpcAcm.Common.Dto
{
    using System.Collections.Generic;
    
    public class JobResult : Job
    {
        public IList<NodeResult> Results { get; private set; }
    }
}