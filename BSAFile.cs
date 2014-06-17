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

        private uint _originalSize;
        public uint OriginalSize
        {
            get
            {
                return IsCompressed ? _originalSize : Size;
            }
            set
            {
                _originalSize = value;
            }
        }

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
            var size = this.Size;
            if (size == 0 || (size <= 4 && IsCompressed))
            {
                _writtenData = new byte[0];
            }

            uint offset;
            using (var reader = createReader())
            {
                if (_settings.BStringPrefixed)
                    reader.BaseStream.Seek(reader.ReadByte() + 1, SeekOrigin.Begin);
                //reader.ReadBString();

                if (IsCompressed)
                    OriginalSize = reader.ReadUInt32();
                //reader.BaseStream.Seek(sizeof(uint), SeekOrigin.Current);

                offset = (uint)reader.BaseStream.Position;
            }

            _readData = new Lazy<byte[]>(() => ReadFileBlock(createReader(), offset, size - offset));
        }

        private BSAFile(string path, string name, ArchiveSettings settings, FileRecord baseRec)
            : this(path, name, settings)
        {
            bool compressBitSet = (baseRec.size & FLAG_COMPRESS) != 0;
            if (compressBitSet)
                baseRec.size ^= FLAG_COMPRESS;

            this.IsCompressed = CheckCompressed(compressBitSet);
            this.Size = baseRec.size;
        }
        private BSAFile(string path, string name, ArchiveSettings settings)
            : this()
        {
            this.Name = FixName(name);
            this.Filename = Path.Combine(FixPath(path), name);
            this._settings = settings;
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

        //private uint CalculateDataSize(uint recordSize)
        //{
        //    if (IsCompressed)
        //        recordSize -= sizeof(uint);
        //    if (_settings.BStringPrefixed)
        //        recordSize -= (uint)Filename.Length + 1;
        //    return recordSize;
        //}

        public uint CalculateRecordSize()
        {
            uint baseSize = this.Size;
            if (_writtenData != null)
            {
                if (IsCompressed)
                    baseSize += sizeof(uint);
                if (_settings.BStringPrefixed)
                    baseSize += (uint)Filename.Length + 1;
            }

            bool setCompressBit = CheckCompressed(IsCompressed);
            if (setCompressBit)
                baseSize |= FLAG_COMPRESS;

            return baseSize;
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
            this.Size = (uint)buf.Length;
        }

        public byte[] GetSaveData(bool extract)
        {
            using (var msOut = new MemoryStream())
            using (var writer = new BinaryWriter(msOut))
            {
                if (_settings.BStringPrefixed && !extract)
                    writer.WriteBString(Filename);

                if (IsCompressed && !extract)
                    writer.Write(OriginalSize);

                writer.Write(GetContents(extract));

                return msOut.ToArray();
            }
        }

        public IEnumerable<byte> YieldSaveData(bool extract)
        {
            const int bufLength = 0x1000;

            using (var msOut = new MemoryStream())
            using (var writer = new BinaryWriter(msOut))
            {
                if (_settings.BStringPrefixed && !extract)
                    writer.WriteBString(Filename);

                if (IsCompressed && !extract)
                    writer.Write(OriginalSize);

                return msOut.ToArray().Concat(YieldContents(extract, bufLength).SelectMany(buf => buf));
            }
        }

        public byte[] GetContents(bool extract)
        {
            if (extract)
            {
                return GetInflatedData(!extract);
            }
            else
            {
                return GetDeflatedData(extract);
            }
        }

        public IEnumerable<byte[]> YieldContents(bool extract, int window)
        {
            if (extract)
            {
                foreach (var buf in YieldInflate(window, !extract))
                    yield return buf;
            }
            else
            {
                foreach (var subBuf in GetDeflatedData(extract).SplitBuffer(window))
                    yield return subBuf;
            }
        }

        private IEnumerable<byte[]> YieldInflate(int window, bool force = false)
        {
            if (OriginalSize == 0)
                yield break;

            if (!IsCompressed ^ force)
                foreach (var subBuf in Data.SplitBuffer(window))
                    yield return subBuf;
            else
            {
                var msData = new MemoryStream(Data);
                var infStream = ZlibDecompressStream(msData, OriginalSize);

                int bytesRead;
                byte[] buf = new byte[window];

                while ((bytesRead = infStream.Read(buf, 0, OriginalSize > window ? window : (int)OriginalSize)) != 0)
                    yield return buf.TrimBuffer(0, bytesRead);

                infStream.Dispose();
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

        private byte[] ReadFileBlock(BinaryReader reader, uint offset, uint size)
        {
            using (reader)
            {
                reader.BaseStream.Seek(offset, SeekOrigin.Begin);
                return reader.ReadBytes((int)size);
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