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
using ICSharpCode.SharpZipLib.Zip.Compression;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;

namespace BSAsharp
{
    /// <summary>
    /// A managed representation of a BSA file record and its contents. BSAFile is not guaranteed to be valid after the BSAReader that created it is disposed.
    /// </summary>
    public class BSAFile : IHashed
    {
        const uint FLAG_COMPRESS = 1 << 30;

        public string Name { get; private set; }
        public string Filename { get; private set; }

        public bool IsCompressed { get; private set; }

        public uint Size
        {
            get
            {
                uint size = (uint)Data.Length;// (uint)GetSaveData(false).Length;

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

        public BSAFile(string path, string name, byte[] data, bool defaultCompressed, bool inputCompressed, bool compressBit = false)
            : this(path, name, defaultCompressed)
        {
            //inputCompressed param specifies compression of data param, NOT final result!
            this.Data = data;
            if (this.IsCompressed = defaultCompressed ^ compressBit)
            {
                this.OriginalSize = (uint)data.Length;
                this.Data = GetDeflatedData(!inputCompressed);
            }
            else
            {
                this.Data = GetInflatedData(inputCompressed);
            }
        }

        internal BSAFile(string path, string name, FileRecord baseRec, BinaryReader reader, bool defaultCompressed, bool preSeek = true, bool leaveOpen = true)
            : this(path, name, baseRec, defaultCompressed)
        {
            this.LeaveOpen = leaveOpen;
            if (preSeek)
                reader.BaseStream.Seek(baseRec.offset, SeekOrigin.Begin);

            ReadFileBlock(reader, baseRec.size);
        }

        private BSAFile(string path, string name, FileRecord baseRec, bool defaultCompressed)
            : this(path, name, defaultCompressed)
        {
            bool compressBitSet = (baseRec.size & FLAG_COMPRESS) != 0;
            if (compressBitSet)
                baseRec.size ^= FLAG_COMPRESS;

            this.IsCompressed = defaultCompressed ^ compressBitSet;
        }
        private BSAFile(string path, string name, bool defaultCompressed)
        {
            this.Name = name.ToLowerInvariant();
            this.Filename = Path.Combine(path, name);
            this.DefaultCompressed = defaultCompressed;

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

        public byte[] GetDeflatedData(bool force = false)
        {
            if (IsCompressed ^ force)
                return this.Data;

            using (var msStream = new MemoryStream(Data))
            {
                return ZlibCompress(msStream);
            }
        }

        public byte[] GetInflatedData(bool force = false)
        {
            if (!IsCompressed ^ force)
                return Data;

            using (var msStream = new MemoryStream(Data))
            {
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
                if (originalSize == 4)
                    //Skip zlib descriptors and ignore header for this file
                    compressedStream.Seek(2, SeekOrigin.Begin);

                using (var infStream = new InflaterInputStream(compressedStream, new Inflater(originalSize == 4)))
                {
                    infStream.CopyTo(msDecompressed);
                }

                return msDecompressed.ToArray();
            }
        }

        private byte[] ZlibCompress(Stream decompressedStream)
        {
            using (MemoryStream msCompressed = new MemoryStream())
            {
                using (var defStream = new DeflaterOutputStream(msCompressed, new Deflater(9)))
                {
                    decompressedStream.CopyTo(defStream);
                }

                return msCompressed.ToArray();
            }
        }
    }
}