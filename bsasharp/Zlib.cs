using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ICSharpCode.SharpZipLib.Zip.Compression;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;

namespace BSAsharp
{
    public static class Zlib
    {
        public static byte[] Decompress(Stream compressedStream, uint originalSize)
        {
            if (originalSize == 0)
                return new byte[0];

            using (var msDecompressed = new MemoryStream((int)originalSize))
            {
                using (var infStream = DecompressStream(compressedStream, originalSize))
                    infStream.CopyTo(msDecompressed);

                return msDecompressed.ToArray();
            }
        }

        public static Stream DecompressStream(Stream compressedStream, uint originalSize)
        {
            if (originalSize == 0)
                throw new ArgumentException("originalSize cannot be 0");

            if (originalSize == 4)
                //Skip zlib descriptors and ignore header for this file
                compressedStream.Seek(2, SeekOrigin.Begin);

            return MakeZlibInflateStream(compressedStream, originalSize == 4);
        }

        private static Stream MakeZlibInflateStream(Stream inStream, bool skipHeader)
        {
            return new InflaterInputStream(inStream, new Inflater(skipHeader));
        }

        public static byte[] Compress(Stream decompressedStream, int level = 6)
        {
            using (MemoryStream msCompressed = new MemoryStream())
            {
                using (var defStream = MakeZlibDeflateStream(msCompressed, level))
                {
                    decompressedStream.CopyTo(defStream);
                }

                return msCompressed.ToArray();
            }
        }

        public static Stream CompressStream(Stream msCompressed, int level = 6)
        {
            return MakeZlibDeflateStream(msCompressed, level);
        }

        private static Stream MakeZlibDeflateStream(Stream outStream, int level)
        {
            //you can substitute any zlib-compatible deflater here
            //gzip, zopfli, etc
            return new DeflaterOutputStream(outStream, new Deflater(level));
        }
    }
}
