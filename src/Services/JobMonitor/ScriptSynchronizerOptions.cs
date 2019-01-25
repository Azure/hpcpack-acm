namespace Microsoft.HpcAcm.Services.JobMonitor
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    class ScriptSynchronizerOptions
    {
        public string GitHubApiBase { get; set; } = "https://api.github.com/repos/Azure/hpcpack-acm/git/trees/release/v1";
        public string GitHubContentBase { get; set; } = "https://raw.githubusercontent.com/Azure/hpcpack-acm/release/v1";
    }
}
