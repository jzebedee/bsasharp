using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO.Compression;
using BSAsharp.Format;
using BSAsharp.Extensions;

namespace BSAsharp
{
    /// <summary>
    /// A managed representation of a BSA file record and its contents. BSAFile is not guaranteed to be valid after the BSAReader that created it is disposed.
    /// </summary>
    public class BSAFile
    {
        const uint FLAG_COMPRESS = 1 << 30;

        public string Name { get; private set; }
        public string Filename { get; private set; }

        public bool IsCompressed { get; private set; }

        public uint Size
        {
            get
            {
                uint size = (uint)GetSaveData(false).Length;

                bool setCompressBit = DefaultCompressed ^ IsCompressed;
                if (setCompressBit)
                    size |= FLAG_COMPRESS;

                return size;
            }
        }
        public uint OriginalSize { get; private set; }

        private readonly Lazy<ulong> _hash;
        public ulong Hash { get { return _hash.Value; } }

        private byte[] Data { get; set; } //data should be "untouched": deflated for IsCompressed files, raw for !IsCompressed

        private readonly bool LeaveOpen;
        private readonly bool DefaultCompressed;

        //public BSAFile(string path, string name, byte[] data, bool compress = true)
        //{
        //    this.Name = name.ToLowerInvariant();
        //    this.Filename = Path.Combine(path.ToLowerInvariant().Replace('/', '\\'), name);

        //    this.Data = data;
        //    this.IsCompressed = compress;
        //}

        internal BSAFile(string path, string name, FileRecord baseRec, BinaryReader reader, bool defaultCompressed, bool preSeek = true, bool leaveOpen = true)
            : this(path, name, baseRec, defaultCompressed)
        {
            this.LeaveOpen = leaveOpen;
            if (preSeek)
                reader.BaseStream.Seek(baseRec.offset, SeekOrigin.Begin);

            ReadFileBlock(reader, baseRec.size);
        }
        private BSAFile(string path, string name, FileRecord baseRec, bool defaultCompressed)
            : this()
        {
            this.Name = name.ToLowerInvariant();
            this.Filename = Path.Combine(path, name);
            this.DefaultCompressed = defaultCompressed;

            bool compressBitSet = (baseRec.size & FLAG_COMPRESS) != 0;
            if (compressBitSet)
                baseRec.size ^= FLAG_COMPRESS;

            this.IsCompressed = defaultCompressed ^ compressBitSet;
        }

        private BSAFile()
        {
            _hash = new Lazy<ulong>(() => Util.CreateHash(Path.GetFileNameWithoutExtension(Name), Path.GetExtension(Name)));
        }

        public byte[] GetSaveData(bool inflate)
        {
            byte[] outData;
            if (IsCompressed)
            {
                if (!inflate)
                {
                    var defData = GetDeflatedData();

                    outData = new byte[sizeof(uint) + defData.Length];

                    var osBytes = BitConverter.GetBytes(OriginalSize);
                    osBytes.CopyTo(outData, 0);

                    defData.CopyTo(outData, sizeof(uint));

                    return outData;
                }
                else
                {
                    return GetInflatedData();
                }
            }

            return Data;
        }

        public byte[] GetDeflatedData()
        {
            if (IsCompressed)
                return this.Data;

            using (var msStream = new MemoryStream(Data))
            {
                return ZlibCompress(msStream);
            }
        }

        public byte[] GetInflatedData()
        {
            if (!IsCompressed)
                return Data;

            using (var msStream = new MemoryStream(Data))
            {
                //Skips zlib descriptors
                msStream.Seek(2, SeekOrigin.Begin);

                var decompressedData = ZlibDecompress(msStream, OriginalSize);

                Trace.Assert(decompressedData.Length == OriginalSize);
                return decompressedData;
            }
        }

        private void ReadFileBlock(BinaryReader reader, uint size)
        {
            //if (BStringPrefixed)
            //{
            //    throw new NotImplementedException();
            //    //var name = reader.ReadBString();
            //}

            if (size == 0 || (size <= 4 && IsCompressed))
            {
                this.Data = new byte[0];
                return;
            }

            if (IsCompressed)
            {
                OriginalSize = reader.ReadUInt32();
                size -= sizeof(uint);

                this.Data = reader.ReadBytes((int)size);
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

        private byte[] ZlibCompress(Stream decompressedStream)
        {
            using (MemoryStream msCompressed = new MemoryStream())
            {
                //DeflateStream closes the underlying stream when disposed
                using (var defStream = new DeflateStream(decompressedStream, CompressionMode.Compress, LeaveOpen))
                {
                    defStream.CopyTo(msCompressed);
                }

                return msCompressed.ToArray();
            }
        }
    }
}