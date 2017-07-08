using System.IO;
using System.IO.Compression;

namespace BSAsharp.Compression
{
    public class ZlibStream : DeflateStream
    {
        private static readonly byte[] HeaderMagic = { 0x78, 0x01 }; //CMF, FLG

        public ZlibStream(Stream stream, CompressionLevel compressionLevel) : base(stream, compressionLevel)
        {
            WriteZlibHeader(stream);
        }

        public ZlibStream(Stream stream, CompressionLevel compressionLevel, bool leaveOpen) : base(stream, compressionLevel, leaveOpen)
        {
            WriteZlibHeader(stream);
        }

        public ZlibStream(Stream stream, CompressionMode mode) : base(stream, mode)
        {
            if (mode == CompressionMode.Decompress) SkipZlibHeader(stream);
            else WriteZlibHeader(stream);
        }

        public ZlibStream(Stream stream, CompressionMode mode, bool leaveOpen) : base(stream, mode, leaveOpen)
        {
            if (mode == CompressionMode.Decompress) SkipZlibHeader(stream);
            else WriteZlibHeader(stream);
        }

        private void SkipZlibHeader(Stream s) => s.Seek(HeaderMagic.Length, SeekOrigin.Current);
        private void WriteZlibHeader(Stream s) => s.Write(HeaderMagic, 0, HeaderMagic.Length);
    }
}
