namespace Microsoft.HpcAcm.Common.Utilities
{
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Table.Protocol;
    using System;
    using System.Collections.Generic;
    using System.Text;

    public static class StorageExceptionExtensions
    {
        public static bool IsConflict(this StorageException ex)
        {
            return ex.RequestInformation.HttpStatusCode == 409 ||
                ex.RequestInformation.HttpStatusCode == 412;
        }
    }
}
