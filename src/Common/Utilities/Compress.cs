namespace Microsoft.HpcAcm.Common.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Text;

    public static class Compress
    {
        public static string GZip(string str)
        {
            var buffer = Encoding.UTF8.GetBytes(str);

            using (var mem = new MemoryStream())
            {
                using (GZipStream zip = new GZipStream(mem, CompressionLevel.Optimal, true))
                {
                    zip.Write(buffer, 0, buffer.Length);
                }

                return Convert.ToBase64String(mem.ToArray());
            }
        }

        public static string UnZip(string base64Str)
        {
            var buffer = Convert.FromBase64String(base64Str);

            using (var mem = new MemoryStream(buffer))
            using (GZipStream zip = new GZipStream(mem, CompressionMode.Decompress, true))
            using (var decompressStream = new MemoryStream())
            {
                zip.CopyTo(decompressStream);
                return Encoding.UTF8.GetString(decompressStream.ToArray());
            }
        }
    }
}
