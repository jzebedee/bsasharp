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

        private Lazy<ulong> _hash;
        public ulong Hash { get { return _hash.Value; } }

        private Lazy<byte[]> _data;
        private byte[] _fixedData;
        private byte[] Data
        { //data should be "untouched": deflated for IsCompressed files, raw for !IsCompressed
            get
            {
                return _fixedData ?? _data.Value;
            }
            set
            {
                _fixedData = value;
            }
        }

        private readonly bool DefaultCompressed;

        public BSAFile(string path, string name, byte[] data, bool defaultCompressed, bool inputCompressed, bool compressBit = false)
            : this(path, name, defaultCompressed)
        {
            //inputCompressed param specifies compression of data param, NOT final result!
            UpdateData(data, inputCompressed, compressBit);
        }

        internal BSAFile(string path, string name, FileRecord baseRec, BinaryReader reader, bool defaultCompressed)
            : this(path, name, baseRec, defaultCompressed)
        {
            _data = new Lazy<byte[]>(() => ReadFileBlock(reader, baseRec.size));
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
            UpdatePath(path, name);
            this.DefaultCompressed = defaultCompressed;
        }

        public void UpdatePath(string path, string name)
        {
            this.Name = name.ToLowerInvariant();
            this.Filename = Path.Combine(path, name);
            _hash = new Lazy<ulong>(() => Util.CreateHash(Path.GetFileNameWithoutExtension(Name), Path.GetExtension(Name)), true);
        }

        public void UpdateData(byte[] buf, bool inputCompressed, bool compressBit = false)
        {
            this.Data = buf;
            if (this.IsCompressed = DefaultCompressed ^ compressBit)
            {
                this.OriginalSize = (uint)buf.Length;
                this.Data = GetDeflatedData(!inputCompressed);
            }
            else
            {
                this.Data = GetInflatedData(inputCompressed);
            }
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

        private byte[] GetDeflatedData(bool force = false)
        {
            if (IsCompressed ^ force)
                return this.Data;

            using (var msStream = new MemoryStream(Data))
            {
                return ZlibCompress(msStream);
            }
        }

        private byte[] GetInflatedData(bool force = false)
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

        private byte[] ReadFileBlock(BinaryReader reader, uint size)
        {
            //if (BStringPrefixed)
            //{
            //    throw new NotImplementedException();
            //    //var name = reader.ReadBString();
            //}

            using (reader)
            {
                if (size == 0 || (size <= 4 && IsCompressed))
                {
                    return new byte[0];
                }

                if (IsCompressed)
                {
                    OriginalSize = reader.ReadUInt32();
                    size -= sizeof(uint);

                    return reader.ReadBytes((int)size);
                }
                else
                {
                    return reader.ReadBytes((int)size);
                }
            }
        }

        private byte[] ZlibDecompress(Stream compressedStream, uint originalSize)
        {
            if (originalSize == 0)
                return new byte[0];

            using (MemoryStream msDecompressed = new MemoryStream((int)originalSize))
            {
                if (originalSize == 4)
                    //Skip zlib descriptors and ignore header for this file
                    compressedStream.Seek(2, SeekOrigin.Begin);

                try
                {
                    using (var infStream = new InflaterInputStream(compressedStream, new Inflater(originalSize == 4)))
                    {
                        infStream.CopyTo(msDecompressed);
                    }

                    return msDecompressed.ToArray();
                }
                catch (ICSharpCode.SharpZipLib.SharpZipBaseException shze)
                {
                    Console.WriteLine("Error inflating " + Filename);
                    Console.Error.WriteLine(shze.ToString());
                    Console.ReadKey();
                }

                return new byte[0];
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