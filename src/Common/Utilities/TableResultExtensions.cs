namespace Microsoft.HpcAcm.Common.Utilities
{
    using Microsoft.WindowsAzure.Storage.Table;
    using System;
    using System.Collections.Generic;
    using System.Text;

    public static class TableResultExtensions
    {
        public static bool IsSuccessfulStatusCode(this TableResult r)
        {
            return r.HttpStatusCode >= 200 && r.HttpStatusCode < 300;
        }
    }
}
