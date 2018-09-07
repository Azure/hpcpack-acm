namespace Microsoft.HpcAcm.Common.Utilities
{
    public class ProcessResult
    {
        public bool Completed { get; set; }
        public int ExitCode { get; set; } = -1;
        public string Output { get; set; }
        public string Error { get; set; }

        public bool IsError { get => this.ExitCode != 0; }
        public string ErrorMessage { get => $"ExitCode: {this.ExitCode}, Error: {this.Error}, Output: {this.Output}"; }
    }
}
