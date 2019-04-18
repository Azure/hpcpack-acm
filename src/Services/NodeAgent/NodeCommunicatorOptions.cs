namespace Microsoft.HpcAcm.Services.NodeAgent
{
    using Microsoft.HpcAcm.Services.Common;

    public class NodeCommunicatorOptions : TaskItemSourceOptions
    {
        public int AutoResendLimit { get; set; } = 1;
        public int ResendIntervalSeconds { get; set; } = 3;
        public string AgentUriBase { get; set; } = "http://localhost:5000/api";
        public string NodeManagerUriBase { get; set; } = "http://localhost:40010/api";
    }
}
