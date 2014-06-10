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

        private readonly bool LeaveOpen;

        public BSAFile(string path, string name, byte[] data, bool compress = true)
        {
            this.Name = name.ToLowerInvariant();
            this.Filename = Path.Combine(path.ToLowerInvariant().Replace('/', '\\'), name);

            this.Data = data;
            this.IsCompressed = compress;
        }

        internal BSAFile(string path, string name, FileRecord baseRec, BinaryReader reader, bool preSeek = true, bool leaveOpen = true)
            : this(path, name, baseRec)
        {
            this.LeaveOpen = leaveOpen;
            if (preSeek)
                reader.BaseStream.Seek(baseRec.offset, SeekOrigin.Begin);
            ReadFileBlock(reader, baseRec.size);
        }
        internal BSAFile(string path, string name, FileRecord baseRec)
        {
            this.Name = name.ToLowerInvariant();
            this.Filename = Path.Combine(path.ToLowerInvariant().Replace('/', '\\'), name);

            bool compressBitSet = (baseRec.size & FLAG_COMPRESS) != 0;
            if (compressBitSet)
                baseRec.size ^= FLAG_COMPRESS;
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
                this.Data = new byte[0];
                return;
            }

            if (IsCompressed)
            {
                var originalSize = reader.ReadUInt32();
                size -= sizeof(uint);

                //Skips zlib descriptors
                reader.BaseStream.Seek(2, SeekOrigin.Current);

                var decompressedData = ZlibDecompress(reader.BaseStream, originalSize);

                Trace.Assert(decompressedData.Length == originalSize);
                this.Data = decompressedData;
            }
            else
            {
                this.Data = reader.ReadBytes((int)size);
                Trace.Assert(this.Data.Length == size);
            }
        }

        private byte[] ZlibDecompress(Stream compressedStream, uint originalSize)
        {
            using (MemoryStream msDecompressed = new MemoryStream((int)originalSize))
            {
                //DeflateStream closes the underlying stream when disposed
                using (var defStream = new DeflateStream(compressedStream, CompressionMode.Decompress, LeaveOpen))
                {
                    defStream.CopyTo(msDecompressed);
                }

                return msDecompressed.ToArray();
            }
        }

        //private byte[] ZlibCompress(byte[], uint originalSize)
        //{
        //    using (MemoryStream msDecompressed = new MemoryStream((int)originalSize))
        //    {
        //        //DeflateStream closes the underlying stream when disposed
        //        using (var defStream = new DeflateStream(compressedStream, CompressionMode.Decompress, LeaveOpen))
        //        {
        //            defStream.CopyTo(msDecompressed);
        //        }

        //        return msDecompressed.ToArray();
        //    }
        //}
    }
}