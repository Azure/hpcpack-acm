namespace Microsoft.HpcAcm.Common.Utilities
{
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Table.Protocol;
    using System;
    using System.Collections.Generic;
    using System.Text;

    public static class StorageExceptionExtensions
    {
        public static bool IsCancellation(this StorageException ex) => ex.InnerException is OperationCanceledException;

        public static bool IsNotFound(this StorageException ex) => string.Equals(ex.RequestInformation.ErrorCode, "MessageNotFound", StringComparison.OrdinalIgnoreCase);

        public static bool IsConflict(this StorageException ex) => ex.RequestInformation.HttpStatusCode == 409 || ex.RequestInformation.HttpStatusCode == 412;
    }
}
