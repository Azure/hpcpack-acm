namespace Microsoft.HpcAcm.Common.Dto
{
    public class NodeResult
    {
        public string Output { get; private set; } 
        public int ExitCode { get; private set; } 
        public string NodeName { get; private set; }
    }
}