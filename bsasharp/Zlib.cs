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

        public byte[] Compress(Stream decompressedStream, CompressionLevel level = CompressionLevel.Optimal)
        {
            using (var msCompressed = new MemoryStream())
            using (var defStream = MakeZlibDeflateStream(msCompressed, level))
            {
                decompressedStream.CopyTo(defStream);
                return msCompressed.ToArray();
            }
        }

        public virtual Stream CompressStream(Stream msCompressed, CompressionLevel level)
        {
            msCompressed.Write(ZLibMagic, 0, ZLibMagic.Length);
            return MakeZlibDeflateStream(msCompressed, level);
        }

        protected virtual Stream MakeZlibDeflateStream(Stream outStream, CompressionLevel level)
        {
            //you can substitute any zlib-compatible deflater here
            //gzip, zopfli, etc
            return new DeflateStream(outStream, level);
        }
    }
}
