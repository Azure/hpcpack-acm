namespace Microsoft.HpcAcm.Common.Utilities
{
    using Microsoft.WindowsAzure.Storage;
    using System;
    using System.Collections.Generic;
    using System.Text;

    public static class RequestResultExtensions
    {
        public static bool IsSuccessfulStatusCode(this RequestResult r)
        {
            return r.HttpStatusCode >= 200 && r.HttpStatusCode < 300;
        }

        public static bool IsConflict(this RequestResult r)
        {
            return r.HttpStatusCode == 412 || r.HttpStatusCode == 409;
        }
    }
}
