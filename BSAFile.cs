using BSAsharp.Format;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO.Compression;

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

        private readonly FileRecord _rec;

        public BSAFile(string name, FileRecord baseRec, BinaryReader reader, bool resetStream = false)
        {
            this.Name = name;
            this._rec = baseRec;

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

        public void CheckFile(Action<FileRecord, byte[]> fileFunction)
        {
            fileFunction(_rec, Data);
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
                var compressedData = reader.ReadBytes((int)size);
                var decompressedData = ZlibDecompress(compressedData, originalSize);

                Trace.Assert(decompressedData.Length == originalSize);
                this.Data = decompressedData;
            }
            else
            {
                this.Data = reader.ReadBytes((int)size);
            }
        }

        private byte[] ZlibDecompress(byte[] inBuf, uint originalSize)
        {
            using (MemoryStream msDecompressed = new MemoryStream((int)originalSize))
            {
                var msCompressed = new MemoryStream(inBuf);
                msCompressed.Seek(2, SeekOrigin.Begin);

                using (var defStream = new DeflateStream(msCompressed, CompressionMode.Decompress))
                {
                    defStream.CopyTo(msDecompressed);
                }

                return msDecompressed.ToArray();
            }
        }
    }
}