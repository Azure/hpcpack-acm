namespace Microsoft.HpcAcm.Services.JobMonitor
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    class ScriptSynchronizerOptions
    {
        public string GitHubApiBase { get; set; } = "https://api.github.com/repos/EvanCui/hpc-acm/git/trees/RC";
        public string GitHubContentBase { get; set; } = "https://raw.githubusercontent.com/EvanCui/hpc-acm/RC";
    }
}
