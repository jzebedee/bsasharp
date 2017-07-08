using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;

namespace BSAsharp.Compression
{
    public class ZlibStream : DeflateStream
    {
        private const int ZlibHeaderSize = 2; //CMF,FLG

        private readonly byte[] Header = new byte[ZlibHeaderSize];

        public int CompressionMethod
        {
            get => Header[0] & 0b1111;
            set => Header[0] |= (byte)(value & 0b1111);
        }
        public int CompressionInfo
        {
            get => (Header[0] & 0b11110000) >> 4;
            set => Header[0] |= (byte)((value << 4) & 0b11110000);
        }

        public int FlagCheck
        {
            get => Header[1] & 0b1111;
            set => Header[1] |= (byte)(value & 0b1111);
        }
        public bool FlagPresetDictionary
        {
            get => (Header[1] & 0b10000) != 0 ? true : false;
            set => Header[1] |= (byte)(value ? 0b10000 : 0);
        }
        public int FlagCompressionLevel
        {
            get => (Header[1] & 0b11000000) >> 6;
            set => Header[1] |= (byte)((value << 6) & 0b11000000);
        }

        public ZlibStream(Stream stream, CompressionLevel compressionLevel) : base(stream, compressionLevel)
        {
            WriteZlibHeader(stream, compressionLevel);
        }

        public ZlibStream(Stream stream, CompressionLevel compressionLevel, bool leaveOpen) : base(stream, compressionLevel, leaveOpen)
        {
            WriteZlibHeader(stream, compressionLevel);
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

        private bool ValidateHeader(byte cmf, byte flg)
        {
            var shortHeader = (ushort)(cmf * 256 + flg);
            return shortHeader % 31 == 0;
        }
        private void ReadZlibHeader(Stream s)
        {
            if (s.Read(Header, 0, Header.Length) != Header.Length)
                throw new InvalidOperationException("Header was not found");

            if (!ValidateHeader(Header[0], Header[1]))
                throw new InvalidOperationException("Header is corrupt");
        }
        private void WriteZlibHeader(Stream s, CompressionLevel? compressionLevel = null)
        {
            if (compressionLevel != null)
            {
                switch (compressionLevel.Value)
                {
                    case CompressionLevel.Fastest:
                        FlagCompressionLevel = 0;
                        CompressionMethod = 8;
                        CompressionInfo = 7;
                        throw new NotImplementedException(nameof(CompressionLevel.Fastest));
                        break;
                    case CompressionLevel.Optimal:
                        CompressionMethod = 8;
                        CompressionInfo = 7;
                        FlagCheck = 0b1010;
                        FlagPresetDictionary = true;
                        FlagCompressionLevel = 3;
                        break;
                    case CompressionLevel.NoCompression:
                        throw new NotImplementedException(nameof(CompressionLevel.NoCompression));
                }
            }

            Debug.Assert(ValidateHeader(Header[0], Header[1]));
            s.Write(Header, 0, Header.Length);
        }
    }
}
