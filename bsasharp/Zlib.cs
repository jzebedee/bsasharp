using System.IO;
using System.IO.Compression;

namespace BSAsharp
{
    public class Zlib
    {
        private static readonly byte[] ZLibMagic = { 0x78, 0x01 }; //CMF, FLG

        public byte[] Decompress(Stream compressedStream)
        {
            using (var msDecompressed = new MemoryStream())
            using (var infStream = DecompressStream(compressedStream))
            {
                infStream.CopyTo(msDecompressed);
                return msDecompressed.ToArray();
            }
        }

        public virtual Stream DecompressStream(Stream compressedStream)
        {
            return MakeZlibInflateStream(compressedStream, true);
        }

        protected virtual Stream MakeZlibInflateStream(Stream inStream, bool skipHeader)
        {
            if (skipHeader)
            {
                inStream.Seek(ZLibMagic.Length, SeekOrigin.Current);
            }
            return new DeflateStream(inStream, CompressionMode.Decompress);
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
            msCompressed.Write(ZLibMagic, 0, ZLibMagic.Length);
            return MakeZlibDeflateStream(msCompressed, level, leaveOpen);
        }

        protected virtual Stream MakeZlibDeflateStream(Stream outStream, CompressionLevel level, bool leaveOpen)
        {
            //you can substitute any zlib-compatible deflater here
            //gzip, zopfli, etc
            return new DeflateStream(outStream, level, leaveOpen);
        }
    }
}
