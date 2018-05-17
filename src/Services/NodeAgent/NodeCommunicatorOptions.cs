namespace Microsoft.HpcAcm.Services.NodeAgent
{
    using Microsoft.HpcAcm.Services.Common;

    public class NodeAgentWorkerOptions : TaskItemSourceOptions
    {
        public int DispatchWorkerCount { get; set; } = 4;
        public int CancelWorkerCount { get; set; } = 2;
        public int SendRequestTimeoutSeconds { get; set; } = 5;
        public int AutoResendLimit { get; set; } = 1;
        public int ResendIntervalSeconds { get; set; } = 3;
        public string AgentUriBase { get; set; } = "http://localhost:8080/api";
        public string NodeManagerUriBase { get; set; } = "http://localhost:40000/api";
    }
}
