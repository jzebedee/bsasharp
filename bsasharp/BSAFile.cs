using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;
using BSAsharp.Format;
using BSAsharp.Extensions;

namespace BSAsharp
{
    /// <summary>
    /// A managed representation of a BSA file record and its contents. BSAFile is not guaranteed to be valid after the BSAReader that created it is disposed.
    /// </summary>
    public class BSAFile : IHashed, ICloneable
    {
        const uint FLAG_COMPRESS = 1 << 30;
        const int DEFLATE_LEVEL_SIZE = 9, DEFLATE_LEVEL_SPEED = 1, DEFLATE_LEVEL_MIXED = 5;

        public string Name { get; private set; }
        public string Filename { get; private set; }

        public bool IsCompressed { get; private set; }

        private bool _forceCompressionChecked = false;
        private int? _optDeflateLevel;

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

        private CompressionOptions Options
        {
            get
            {
                return _settings.Options;
            }
        }

        private CompressionStrategy Strategy
        {
            get
            {
                return Options.Strategy;
            }
        }

        public BSAFile(string path, string name, ArchiveSettings settings, byte[] data, bool inputCompressed)
            : this(path, name, settings)
        {
            //inputCompressed param specifies compression of data param, NOT final result!
            UpdateData(data, inputCompressed);
        }

        internal BSAFile(string path, string name, ArchiveSettings settings, FileRecord baseRec, Func<int, int, Stream> createStream)
            : this(path, name, settings, baseRec)
        {
            if (Size == 0 || (Size <= 4 && IsCompressed))
                _writtenData = new byte[0];

            int offset = _settings.BStringPrefixed ? Filename.Length + 1 : 0;
            if (IsCompressed)
            {
                using (var reader = new BinaryReader(createStream(offset, sizeof(uint))))
                    OriginalSize = reader.ReadUInt32();
                offset += sizeof(uint);
            }

            this._createStream = () => createStream(offset, (int)Size - offset);
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

            CheckCompressionSettings();

            if (Strategy.HasFlag(CompressionStrategy.Aggressive))
                throw new NotImplementedException("CompressionStrategy.Aggressive");
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

            CheckCompressionSettings();
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
            ForceCompressionIfNeeded();

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

        private void CheckCompressionSettings()
        {
            _optDeflateLevel = Options.GetCompressionLevel(Path.GetExtension(Name));
            if (_optDeflateLevel.HasValue && _optDeflateLevel.Value < 0)
            {
                //blocks compression changing on this file
                _forceCompressionChecked = true;
            }
            else
            {
                _forceCompressionChecked = false;
            }
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
            CheckCompressionSettings();
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

        private void ForceCompressionIfNeeded()
        {
            if (Strategy.HasFlag(CompressionStrategy.Unsafe) && !_forceCompressionChecked)
            {
                _forceCompressionChecked = true;
                if (!IsCompressed || Strategy.HasFlag(CompressionStrategy.Aggressive))
                {
                    UpdateData(GetContents(true), false, !IsCompressed);
                }
            }
        }

        public MemoryStream GetSaveStream()
        {
            ForceCompressionIfNeeded();
            return GetRawSaveStream();
        }
        private MemoryStream GetRawSaveStream()
        {
            var msOut = new MemoryStream();

            //http://stackoverflow.com/questions/12182202/should-i-dispose-a-binaryreader-if-i-need-to-preserve-the-wrapped-stream
            //thanks Skeeter
            var writer = new BinaryWriter(msOut);
            if (_settings.BStringPrefixed)
                writer.WriteBString(Filename);

            if (IsCompressed)
                writer.Write(OriginalSize);

            writer.Write(GetContents(!IsCompressed));

            return msOut;
        }

        public byte[] GetSaveData()
        {
            using (var msOut = GetSaveStream())
                return msOut.ToArray();
        }
        private byte[] GetRawSaveData()
        {
            using (var msOut = GetRawSaveStream())
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
                    return Zlib.DecompressStream(GetDataStream(), OriginalSize);
                }
            }
            else
            {
                if (!IsCompressed || force)
                {
                    return Zlib.CompressStream(GetDataStream(), GetDeflateLevel());
                }
            }

            return GetDataStream();
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

                    stream.Read(buf, 0, buf.Length);

                    return buf;
                }
            }

            throw new InvalidOperationException("GetData() had no data source");
        }

        private byte[] GetDeflatedData(bool force = false)
        {
            if (!IsCompressed || force)
                using (var dataStream = GetDataStream())
                {
                    return Zlib.Compress(dataStream, GetDeflateLevel());
                }

            return GetData();
        }

        private byte[] GetInflatedData(bool force = false)
        {
            if (IsCompressed || force)
                using (var dataStream = GetDataStream())
                {
                    var decompressedData = Zlib.Decompress(dataStream, OriginalSize);

                    if (decompressedData.Length != OriginalSize)
                        throw new IOException("Inflated size did not match original size");

                    return decompressedData;
                }

            return GetData();
        }

        private int GetDeflateLevel()
        {
            if (_optDeflateLevel.HasValue)
                return _optDeflateLevel.Value;

            if (Strategy.HasFlag(CompressionStrategy.Size | CompressionStrategy.Speed))
                return DEFLATE_LEVEL_MIXED;
            if (Strategy.HasFlag(CompressionStrategy.Speed))
                return DEFLATE_LEVEL_SPEED;
            if (Strategy.HasFlag(CompressionStrategy.Size))
                return DEFLATE_LEVEL_SIZE;

            throw new ArgumentException("CompressionStrategy did not have enough information");
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