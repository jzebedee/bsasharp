using System;
using System.IO;
using System.IO.Compression;

namespace BSAsharp.Compression
{
    public class ZlibStream : DeflateStream
    {
        private const int ZlibHeaderSize = 2; //CMF,FLG

        private readonly byte[] Header = new byte[ZlibHeaderSize];

        public int CompressionMethod => Header[0] & 0b1111;

        public int CompressionInfo => (Header[0] & 0b11110000) >> 4;

        public int FlagCheck => Header[1] & 0b1111;

        public bool FlagPresetDictionary => (Header[1] & 0b10000) != 0 ? true : false;

        public int FlagCompressionLevel => (Header[1] & 0b11000000) >> 6;

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
            if (mode == CompressionMode.Decompress) ReadZlibHeader(stream);
            else WriteZlibHeader(stream);
        }

        public ZlibStream(Stream stream, CompressionMode mode, bool leaveOpen) : base(stream, mode, leaveOpen)
        {
            if (mode == CompressionMode.Decompress) ReadZlibHeader(stream);
            else WriteZlibHeader(stream);
        }

        private void ReadZlibHeader(Stream s)
        {
            if (s.Read(Header, 0, Header.Length) != Header.Length)
                throw new InvalidOperationException("Header was not found");

            var shortHeader = (ushort)(Header[0] * 256 + Header[1]);
            if (shortHeader % 31 != 0)
                throw new InvalidOperationException("Header is corrupt");
        }
        private void WriteZlibHeader(Stream s) => s.Write(Header, 0, Header.Length);
    }
}
