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
        const int DEFLATE_LEVEL = 9;

        public string Name { get; private set; }
        public string Filename { get; private set; }

        public bool IsCompressed { get; private set; }

        public uint Size { get; private set; }
        public uint OriginalSize { get; private set; }

        //hash MUST be immutable due to undefined behavior when the sort changes in a SortedSet<T>
        private readonly Lazy<ulong> _hash;
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

        public BSAFile(string path, string name, ArchiveSettings settings, byte[] data, bool inputCompressed)
            : this(path, name, settings)
        {
            //inputCompressed param specifies compression of data param, NOT final result!
            UpdateData(data, inputCompressed);
        }

        internal BSAFile(string path, string name, ArchiveSettings settings, FileRecord baseRec, Func<BinaryReader> createReader)
            : this(path, name, settings, baseRec)
        {
            _readData = new Lazy<byte[]>(() => ReadFileBlock(createReader(), baseRec.size));
        }

        //Clone ctor
        private BSAFile(string path, string name, ArchiveSettings settings, Lazy<byte[]> lazyData, byte[] writtenData, bool isCompressed, uint originalSize, uint size)
            : this()
        {
            this.Name = FixName(name);
            this.Filename = Path.Combine(FixPath(path), this.Name);

            this._settings = settings;
            this._writtenData = writtenData;
            this._readData = lazyData;
            this.IsCompressed = isCompressed;
            this.OriginalSize = originalSize;
            this.Size = size;
        }
        private BSAFile(string path, string name, ArchiveSettings settings, FileRecord baseRec)
            : this(path, name, settings)
        {
            bool compressBitSet = (baseRec.size & FLAG_COMPRESS) != 0;
            if (compressBitSet)
                baseRec.size ^= FLAG_COMPRESS;

            this.IsCompressed = CheckCompressed(compressBitSet);
            this.Size = CalculateSize(baseRec.size);
        }
        private BSAFile(string path, string name, ArchiveSettings settings)
            : this()
        {
            this.Name = FixName(name);
            this.Filename = Path.Combine(FixPath(path), name);
            this._settings = settings;
        }
        private BSAFile()
        {
            _hash = new Lazy<ulong>(MakeHash);
        }

        public override string ToString()
        {
            return Filename;
        }

        public static string FixName(string name)
        {
            return name.ToLowerInvariant();
        }

        public static string FixPath(string path)
        {
            return path.ToLowerInvariant().Replace('/', '\\');
        }

        private uint CalculateSize(uint baseSize, bool fromBaseData = false)
        {
            uint size = baseSize;

            if (fromBaseData)
            {
                if (IsCompressed)
                    size += sizeof(uint);
                if (_settings.BStringPrefixed)
                    size += (uint)Filename.Length + 1;
            }

            bool setCompressBit = CheckCompressed(IsCompressed);
            if (setCompressBit)
                size |= FLAG_COMPRESS;

            return size;
        }

        private ulong MakeHash()
        {
            return Util.CreateHash(Path.GetFileNameWithoutExtension(Name), Path.GetExtension(Name));
        }

        private bool CheckCompressed(bool compressBitSet)
        {
            return _settings.DefaultCompressed ^ compressBitSet;
        }

        public void UpdateData(byte[] buf, bool inputCompressed)
        {
            this.IsCompressed = inputCompressed;
            this.Data = buf;

            if (this.IsCompressed)
            {
                this.Data = GetDeflatedData(!inputCompressed);
            }
            else
            {
                this.Data = GetInflatedData(inputCompressed);
                this.OriginalSize = (uint)this.Data.Length;
            }
            this.Size = CalculateSize((uint)buf.Length, true);
        }

        public byte[] GetSaveData(bool extract)
        {
            using (var msOut = new MemoryStream())
            using (var writer = new BinaryWriter(msOut))
            {
                if (_settings.BStringPrefixed && !extract)
                    writer.WriteBString(Filename);

                if (IsCompressed)
                {
                    if (!extract)
                    {
                        var defData = GetDeflatedData();

                        writer.Write(OriginalSize);
                        writer.Write(defData);
                    }
                    else
                    {
                        writer.Write(GetInflatedData());
                    }
                }
                else
                {
                    writer.Write(Data);
                }

                return msOut.ToArray();
            }
        }
        public IEnumerable<byte> GetYieldingSaveData(bool extract)
        {
            MemoryStream msData = new MemoryStream(Data);

            using (var msOut = new MemoryStream())
            using (var writer = new BinaryWriter(msOut))
                if (_settings.BStringPrefixed && !extract)
                {
                    writer.WriteBString(Filename);
                    var bStringBuf = msOut.ToArray();

                    foreach (var bsbyte in bStringBuf)
                        yield return bsbyte;
                }

            if (IsCompressed)
            {
                if (!extract)
                {
                    using (var msOut = new MemoryStream())
                    using (var writer = new BinaryWriter(msOut))
                    {
                        writer.Write(OriginalSize);
                        var oSizeBuf = msOut.ToArray();

                        foreach (var osbyte in oSizeBuf)
                            yield return osbyte;
                    }

                    foreach (var dbyte in Data)
                        yield return dbyte;
                }
                else
                {
                    var infStream = ZlibDecompressStream(msData, OriginalSize);

                    int ibyte;
                    while ((ibyte = infStream.ReadByte()) != -1)
                        yield return (byte)ibyte;

                    infStream.Dispose();
                }
            }
            else
            {
                foreach (var dbyte in Data)
                    yield return dbyte;
            }

            if (msData != null)
            {
                msData.Dispose();
            }
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

            using (var msDecompressed = new MemoryStream((int)originalSize))
            {
                using (var infStream = ZlibDecompressStream(compressedStream, originalSize))
                    infStream.CopyTo(msDecompressed);

                return msDecompressed.ToArray();
            }
        }

        private Stream ZlibDecompressStream(Stream compressedStream, uint originalSize)
        {
            if (originalSize == 0)
                throw new ArgumentException("originalSize cannot be 0");

            if (originalSize == 4)
                //Skip zlib descriptors and ignore header for this file
                compressedStream.Seek(2, SeekOrigin.Begin);

            return MakeZlibInflateStream(compressedStream, originalSize == 4);
        }

        private static Stream MakeZlibInflateStream(Stream inStream, bool skipHeader)
        {
            return new InflaterInputStream(inStream, new Inflater(skipHeader));
        }

        private byte[] ZlibCompress(Stream decompressedStream)
        {
            using (MemoryStream msCompressed = new MemoryStream())
            {
                using (var defStream = MakeZlibDeflateStream(msCompressed))
                {
                    decompressedStream.CopyTo(defStream);
                }

                return msCompressed.ToArray();
            }
        }

        private static Stream MakeZlibDeflateStream(Stream outStream)
        {
            return new DeflaterOutputStream(outStream, new Deflater(DEFLATE_LEVEL));
        }

        public object Clone()
        {
            return new BSAFile(Filename, Name, _settings, _readData, _writtenData, IsCompressed, OriginalSize, Size);
        }

        public BSAFile DeepCopy(string newPath = null, string newName = null)
        {
            return new BSAFile(newPath ?? Path.GetDirectoryName(Filename), newName ?? Name, _settings, _readData, _writtenData, IsCompressed, OriginalSize, Size);
        }
    }
}