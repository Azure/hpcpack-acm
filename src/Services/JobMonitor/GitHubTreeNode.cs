namespace Microsoft.HpcAcm.Services.JobMonitor
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    class GitHubTree
    {
        public class GitHubTreeNode
        {
            public string path { get; set; }
            public string url { get; set; }
        }

        public List<GitHubTreeNode> tree { get; set; }
    }
}
