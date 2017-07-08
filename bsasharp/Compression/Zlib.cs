using System.IO;
using System.IO.Compression;

namespace BSAsharp.Compression
{
    public class Zlib
    {
        public byte[] Decompress(Stream compressedStream)
        {
            using (var msDecompressed = new MemoryStream())
            using (var infStream = new ZlibStream(compressedStream, CompressionMode.Decompress))
            {
                infStream.CopyTo(msDecompressed);
                return msDecompressed.ToArray();
            }
        }

        //public byte[] Compress(Stream inputStream, CompressionLevel level = CompressionLevel.Optimal, bool leaveOpen = true)
        //{
        //    using (var msCompressed = new MemoryStream())
        //    using (var defStream = MakeZlibDeflateStream(msCompressed, level, leaveOpen))
        //    {
        //        inputStream.CopyTo(defStream);
        //        return msCompressed.ToArray();
        //    }
        //}

        public virtual Stream CompressStream(Stream msCompressed, CompressionLevel level, bool leaveOpen = true)
        {
            return new ZlibStream(msCompressed, level, leaveOpen);
        }
    }
}
