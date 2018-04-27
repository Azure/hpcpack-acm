namespace Microsoft.HpcAcm.Common.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    public static class IntegerKey
    {
        public static string ToStringKey(int key) => key.ToString("D10");
        public static string ToStringKey(long key) => key.ToString("D19");
    }
}
