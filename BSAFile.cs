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
    /// <summary>
    /// A managed representation of a BSA file record and its contents. BSAFile is not guaranteed to be valid after the BSAReader that created it is disposed.
    /// </summary>
    public class BSAFile
    {
        const uint FLAG_COMPRESS = 1 << 30;

        public static bool DefaultCompressed { get; set; }
        public static bool BStringPrefixed { get; set; }

        public string Name { get; private set; }
        public string Filename { get; private set; }
        public bool IsCompressed { get; private set; }
        public byte[] Data { get; private set; }

        internal BSAFile(string path, string name, FileRecord baseRec, BinaryReader reader)
            : this(path, name, baseRec)
        {
            reader.BaseStream.Seek(baseRec.offset, SeekOrigin.Begin);
            ReadFileBlock(reader, baseRec.size);
        }
        internal BSAFile(string path, string name, FileRecord baseRec, UnmanagedMemoryAccessor accessor)
            : this(path, name, baseRec)
        {
            ReadFileBlock(accessor, baseRec.size);
        }

        private BSAFile(string path, string name, FileRecord baseRec)
        {
            this.Name = name;
            this.Filename = Path.Combine(path, name);

            bool compressBitSet = (baseRec.size & FLAG_COMPRESS) != 0;
            this.IsCompressed = DefaultCompressed ^ compressBitSet;
        }

        private void ReadFileBlock(BinaryReader reader, uint size)
        {
            if (BStringPrefixed)
            {
                throw new NotImplementedException();
                //var name = reader.ReadBString();
            }

            if (size == 0 || (size <= 4 && IsCompressed))
            {
                this.Data = new byte[size];
                return;
            }

            if (IsCompressed)
            {
                var originalSize = reader.ReadUInt32();
                size -= sizeof(uint);

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

        private void ReadFileBlock(UnmanagedMemoryAccessor reader, uint size)
        {
            using (reader)
            {
                if (BStringPrefixed)
                {
                    throw new NotImplementedException();
                    //var name = reader.ReadBString();
                }

                if (size == 0 || (size <= 4 && IsCompressed))
                {
                    this.Data = new byte[size];
                    return;
                }

                if (IsCompressed)
                {
                    var originalSize = reader.ReadUInt32(0);
                    size -= sizeof(uint);

                    byte[] compressedData = new byte[size];
                    int bytesRead = reader.ReadArray<byte>(sizeof(uint), compressedData, 0, (int)size);
                    Trace.Assert(bytesRead == size);

                    var decompressedData = ZlibDecompress(compressedData, originalSize);

                    Trace.Assert(decompressedData.Length == originalSize);
                    this.Data = decompressedData;
                }
                else
                {
                    this.Data = new byte[size];
                    int bytesRead = reader.ReadArray<byte>(0, this.Data, 0, (int)size);
                    Trace.Assert(bytesRead == size);
                }
            }
        }

        private byte[] ZlibDecompress(byte[] inBuf, uint originalSize)
        {
            using (MemoryStream msDecompressed = new MemoryStream((int)originalSize))
            {
                var msCompressed = new MemoryStream(inBuf);

                //Skips zlib descriptors
                msCompressed.Seek(2, SeekOrigin.Begin);

                //DeflateStream closes the underlying stream when disposed
                using (var defStream = new DeflateStream(msCompressed, CompressionMode.Decompress))
                {
                    defStream.CopyTo(msDecompressed);
                }

                return msDecompressed.ToArray();
            }
        }
    }
}