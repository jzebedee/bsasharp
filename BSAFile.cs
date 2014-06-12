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
    public class BSAFile : IHashed, ICloneable
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

                bool setCompressBit = _settings.DefaultCompressed ^ IsCompressed;
                if (setCompressBit)
                    size |= FLAG_COMPRESS;

                return size;
            }
        }
        public uint OriginalSize { get; private set; }

        //hash MUST be immutable due to undefined behavior when the sort changes in a SortedSet<T>
        //private readonly Lazy<ulong> _hash;
        private Lazy<ulong> _hash; //fuck safety
        public ulong Hash { get { return _hash.Value; } }

        private Lazy<byte[]> _readData;
        private byte[] _writtenData;
        private byte[] Data
        { //data should be "untouched": deflated for IsCompressed files, raw for !IsCompressed
            get
            {
                return _writtenData ?? _readData.Value;
            }
            set
            {
                _writtenData = value;
            }
        }

        private readonly ArchiveSettings _settings;

        public BSAFile(string path, string name, ArchiveSettings settings, byte[] data, bool inputCompressed, bool compressBit = false)
            : this(path, name, settings)
        {
            //inputCompressed param specifies compression of data param, NOT final result!
            UpdateData(data, inputCompressed, compressBit);
        }

        internal BSAFile(string path, string name, ArchiveSettings settings, FileRecord baseRec, BinaryReader reader)
            : this(path, name, settings, baseRec)
        {
            _readData = new Lazy<byte[]>(() => ReadFileBlock(reader, baseRec.size));
            //Trace.Assert(baseRec.hash == Hash);
        }

        //Clone ctor
        private BSAFile(string fileName, string name, ArchiveSettings settings, Lazy<byte[]> lazyData, byte[] writtenData, bool isCompressed, uint originalSize)
        {
            this.Filename = fileName;
            this.Name = name;
            UpdateHash();

            this._settings = settings;
            this._writtenData = writtenData;
            this._readData = lazyData;
            this.IsCompressed = isCompressed;
            this.OriginalSize = originalSize;
        }
        private BSAFile(string path, string name, ArchiveSettings settings, FileRecord baseRec)
            : this(path, name, settings)
        {
            bool compressBitSet = (baseRec.size & FLAG_COMPRESS) != 0;
            if (compressBitSet)
                baseRec.size ^= FLAG_COMPRESS;

            this.IsCompressed = settings.DefaultCompressed ^ compressBitSet;
        }
        private BSAFile(string path, string name, ArchiveSettings settings)
        {
            UpdatePath(path, name);
            this._settings = settings;
        }

        public void UpdatePath(string path, string name)
        {
            this.Name = name.ToLowerInvariant();
            this.Filename = Path.Combine(path.ToLowerInvariant().Replace('/', '\\'), name);
            UpdateHash();
        }
        private void UpdateHash()
        {
            _hash = new Lazy<ulong>(() => Util.CreateHash(Path.GetFileNameWithoutExtension(Name), Path.GetExtension(Name)), true);
        }

        public void UpdateData(byte[] buf, bool inputCompressed, bool compressBit = false)
        {
            this.Data = buf;
            if (this.IsCompressed = _settings.DefaultCompressed ^ compressBit)
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
            if (_settings.BStringPrefixed)
            {
                var length = reader.ReadByte();
                reader.BaseStream.Seek(length, SeekOrigin.Current);
                //var name = reader.ReadBString();
                //var newHash = Util.CreateHash(Path.GetFileNameWithoutExtension(name), Path.GetExtension(name));
                //Console.WriteLine(newHash == Hash);
                //Console.WriteLine(name);
            }

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

        public object Clone()
        {
            return new BSAFile(Filename, Name, _settings, _readData, _writtenData, IsCompressed, OriginalSize);
        }
        
        public BSAFile DeepCopy()
        {
            return Clone() as BSAFile;
        }
    }
}