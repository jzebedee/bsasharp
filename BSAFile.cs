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

        private byte[] _writtenData;
        private Func<Stream> _createStream;

        private readonly ArchiveSettings _settings;

        public BSAFile(string path, string name, ArchiveSettings settings, byte[] data, bool inputCompressed)
            : this(path, name, settings)
        {
            //inputCompressed param specifies compression of data param, NOT final result!
            UpdateData(data, inputCompressed);
        }

        internal BSAFile(string path, string name, ArchiveSettings settings, FileRecord baseRec, Func<int, int, Stream> createStream)
            : this(path, name, settings, baseRec)
        {
            var size = (int)this.Size;
            if (size == 0 || (size <= 4 && IsCompressed))
            {
                _writtenData = new byte[0];
            }

            int offset = _settings.BStringPrefixed ? Filename.Length + 1 : 0;
            if (IsCompressed)
            {
                using (var reader = new BinaryReader(createStream(offset, sizeof(uint))))
                    OriginalSize = reader.ReadUInt32();
                offset += sizeof(uint);
            }

            this._createStream = () => createStream(offset, size - offset);
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
        private BSAFile(string path, string name, ArchiveSettings settings, Func<Stream> createStream, byte[] writtenData, bool isCompressed, uint originalSize, uint size)
            : this()
        {
            this.Name = FixName(name);
            this.Filename = Path.Combine(FixPath(path), this.Name);

            this._settings = settings;

            this._writtenData = writtenData;
            this._createStream = createStream;

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

        public void UpdateData(byte[] buf, bool inputCompressed, bool flipCompression = false)
        {
            this.IsCompressed = CheckCompressed(flipCompression);
            this._writtenData = buf;

            if (this.IsCompressed)
            {
                if (!inputCompressed)
                    this.OriginalSize = (uint)buf.Length;
                this._writtenData = GetDeflatedData(!inputCompressed);
            }
            else
            {
                this._writtenData = GetInflatedData(inputCompressed);
                this.OriginalSize = (uint)_writtenData.Length;
            }
            this.Size = (uint)_writtenData.Length;
        }

        public MemoryStream GetSaveStream(bool extract)
        {
            var msOut = new MemoryStream();

            //http://stackoverflow.com/questions/12182202/should-i-dispose-a-binaryreader-if-i-need-to-preserve-the-wrapped-stream
            //thanks Skeeter
            var writer = new BinaryWriter(msOut);
            if (_settings.BStringPrefixed && !extract)
                writer.WriteBString(Filename);

            if (IsCompressed && !extract)
                writer.Write(OriginalSize);

            writer.Write(GetContents(!IsCompressed || extract));

            return msOut;
        }

        public byte[] GetSaveData(bool extract)
        {
            using (var msOut = GetSaveStream(extract))
                return msOut.ToArray();
        }

        public byte[] GetContents(bool extract, bool force = false)
        {
            if (extract)
            {
                return GetInflatedData(force);
            }
            else
            {
                return GetDeflatedData(force);
            }
        }

        public Stream GetContentStream(bool extract, bool force = false)
        {
            if (extract)
            {
                if (OriginalSize == 0)
                    return null;

                if (IsCompressed || force)
                {
                    return ZlibDecompressStream(GetDataStream(), OriginalSize);
                }
            }
            else
            {
                if (!IsCompressed || force)
                {
                    return ZlibCompressStream(GetDataStream());
                }
            }

            return GetDataStream();
        }

        public IEnumerable<byte[]> YieldContents(bool extract, int window, bool force = false)
        {
            if (extract)
            {
                if (OriginalSize == 0)
                    return new byte[0][];

                return YieldInflate(window, force);
            }
            else
            {
                return YieldDeflate(window, force);
            }
        }

        private IEnumerable<byte[]> YieldInflate(int window, bool force = false)
        {
            if (IsCompressed || force)
            {
                foreach (var slice in ReadToWindow(ZlibDecompressStream(GetDataStream(), OriginalSize), OriginalSize, window))
                    yield return slice;
            }
            else
            {
                foreach (var slice in ReadToWindow(GetDataStream(), OriginalSize, window))
                    yield return slice;
            }
        }

        private Stream GetDataStream()
        {
            if (_writtenData != null)
                return new MemoryStream(_writtenData);
            else if (_createStream != null)
                return _createStream();

            throw new InvalidOperationException("GetDataStream() had no data source");
        }

        private byte[] GetData()
        {
            if (_writtenData != null)
                return _writtenData;

            if (_createStream != null)
            {
                using (var stream = _createStream())
                {
                    byte[] buf = new byte[stream.Length];

                    int bytesRead = stream.Read(buf, 0, buf.Length);
                    Trace.Assert(bytesRead == buf.Length);

                    return buf;
                }
            }

            throw new InvalidOperationException("GetData() had no data source");
        }

        private IEnumerable<byte[]> ReadToWindow(Stream stream, uint size, int window)
        {
            int bytesRead;
            byte[] buf = new byte[window];

            using (stream)
                while ((bytesRead = stream.Read(buf, 0, size > window ? window : (int)size)) != 0)
                    yield return buf.TrimBuffer(0, bytesRead);
        }

        private byte[] GetDeflatedData(bool force = false)
        {
            if (!IsCompressed || force)
                using (var dataStream = GetDataStream())
                {
                    return ZlibCompress(dataStream);
                }

            return GetData();
        }

        private IEnumerable<byte[]> YieldDeflate(int window, bool force = false)
        {
            if (!IsCompressed || force)
            {
                foreach (var slice in ReadToWindow(ZlibCompressStream(GetDataStream()), Size, window))
                    yield return slice;
            }
            else
            {
                foreach (var slice in ReadToWindow(GetDataStream(), Size, window))
                    yield return slice;
            }
        }

        private byte[] GetInflatedData(bool force = false)
        {
            if (IsCompressed || force)
                using (var dataStream = GetDataStream())
                {
                    var decompressedData = ZlibDecompress(dataStream, OriginalSize);

                    Trace.Assert(decompressedData.Length == OriginalSize);
                    return decompressedData;
                }

            return GetData();
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

        private Stream ZlibCompressStream(Stream msCompressed)
        {
            return MakeZlibDeflateStream(msCompressed);
        }

        private static Stream MakeZlibDeflateStream(Stream outStream)
        {
            return new DeflaterOutputStream(outStream, new Deflater(DEFLATE_LEVEL));
        }

        public object Clone()
        {
            return new BSAFile(Filename, Name, _settings, _createStream, _writtenData, IsCompressed, OriginalSize, Size);
        }

        public BSAFile DeepCopy(string newPath = null, string newName = null)
        {
            return new BSAFile(newPath ?? Path.GetDirectoryName(Filename), newName ?? Name, _settings, _createStream, _writtenData, IsCompressed, OriginalSize, Size);
        }
    }
}