using BSAsharp.Format;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Diagnostics;
using ICSharpCode.SharpZipLib;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using ICSharpCode.SharpZipLib.Zip.Compression;

namespace BSAsharp
{
    class BSAFile
    {
        const uint FLAG_COMPRESS = 1 << 30;

        public static bool DefaultCompressed { get; set; }
        public static bool BStringPrefixed { get; set; }

        public string Name { get; private set; }
        public bool IsCompressed { get; private set; }
        public byte[] Data { get; private set; }

        public BSAFile(string name, FileRecord baseRec, BinaryReader reader, bool resetStream = false)
        {
            this.Name = name;

            bool compressBitSet = (baseRec.size & FLAG_COMPRESS) != 0;
            this.IsCompressed = DefaultCompressed ^ compressBitSet;

            long streamPos = reader.BaseStream.Position;
            try
            {
                reader.BaseStream.Seek(baseRec.offset, SeekOrigin.Begin);
                ReadFileBlock(reader, baseRec.size);
            }
            finally
            {
                if (resetStream)
                    reader.BaseStream.Seek(streamPos, SeekOrigin.Begin);
            }
        }

        private void ReadFileBlock(BinaryReader reader, uint size)
        {
            if (BStringPrefixed)
            {
                var name = reader.ReadBString();
            }

            if (IsCompressed)
            {
                var originalSize = reader.ReadUInt32();
                var compressedData = reader.ReadBytes(sizeof(uint) + (int)size);

                var decompressedData = ZlibDecompress(compressedData);

                Trace.Assert(decompressedData.Length == originalSize);
                this.Data = decompressedData;
            }
            else
            {
                this.Data = reader.ReadBytes((int)size);
            }
        }

        private byte[] ZlibDecompress(byte[] inBuf)
        {
            using (MemoryStream msCompressed = new MemoryStream(inBuf), msDecompressed = new MemoryStream())
            {
                using (var zlStream = new InflaterInputStream(msCompressed))
                {
                    zlStream.CopyTo(msDecompressed);
                }

                return msDecompressed.ToArray();
            }
        }
    }
}