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
    [DebuggerDisplay("{Filename}")]
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

        private uint? _originalSize;
        public uint OriginalSize
        {
            get
            {
                if (IsCompressed)
                {
                    if (_originalSize == null)
                        GetData();
                    Debug.Assert(_originalSize != null);

                    return _originalSize.Value;
                }

                return Size;
            }
            private set
            {
                _originalSize = value;
            }
        }

        //hash MUST be immutable due to undefined behavior when the sort changes in a SortedSet<T>
        private readonly ulong _hash;
        public ulong Hash { get { return _hash; } }

        private byte[] _readyData;
        private Lazy<byte[]> _lazyData;

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

        internal BSAFile(string path, string name, ArchiveSettings settings, FileRecord baseRec, Func<uint, uint, BinaryReader> createReader)
            : this(path, name, settings, baseRec)
        {
            if (Size == 0 || (Size <= 4 && IsCompressed))
                _readyData = new byte[0];

            uint offset = _settings.BStringPrefixed ? (uint)Filename.Length + 1 : 0;
            _lazyData = new Lazy<byte[]>(() =>
            {
                using (var reader = createReader(offset, Size - offset))
                {
                    if (IsCompressed)
                    {
                        _originalSize = reader.ReadUInt32();
                        offset += sizeof(uint);
                    }

                    return reader.ReadBytes((int)(Size - offset));
                }
            });
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
            : this(path, name)
        {
            this._settings = settings;

            CheckCompressionSettings();

            if (Strategy.HasFlag(CompressionStrategy.Aggressive))
                throw new NotImplementedException("CompressionStrategy.Aggressive");
        }

        //Clone ctor
        private BSAFile(string path, string name, ArchiveSettings settings, byte[] readyData, Lazy<byte[]> lazyData, bool isCompressed, uint originalSize, uint size)
            : this(path, name)
        {
            this._settings = settings;

            this._readyData = readyData;
            this._lazyData = lazyData;

            this.IsCompressed = isCompressed;

            this.OriginalSize = originalSize;
            this.Size = size;

            CheckCompressionSettings();
        }
        private BSAFile(string path, string name)
        {
            this.Name = name.ToLowerInvariant();
            this.Filename = Path.Combine(Util.FixPath(path), this.Name);

            _hash = MakeHash();
        }

        public override string ToString()
        {
            return Filename;
        }

        public uint CalculateRecordSize()
        {
            ForceCompressionIfNeeded();

            uint baseSize = this.Size;

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
            _lazyData = null;

            CheckCompressionSettings();
            IsCompressed = CheckCompressed(flipCompression);
            _readyData = buf;

            if (this.IsCompressed)
            {
                if (!inputCompressed)
                    OriginalSize = (uint)buf.Length;
                _readyData = GetDeflatedData(!inputCompressed);
            }
            else
            {
                _readyData = GetInflatedData(inputCompressed);
                OriginalSize = (uint)_readyData.Length;
            }

            Size = (uint)_readyData.Length;
            //d3c09556
            if (IsCompressed)
                Size += sizeof(uint);
            if (_settings.BStringPrefixed)
                Size += (uint)Filename.Length + 1;
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

        public void Cache()
        {
            ForceCompressionIfNeeded();
        }

        private Stream GetDataStream()
        {
            return new MemoryStream(GetData());
        }

        private byte[] GetData()
        {
            if (_readyData == null)
            {
                if (_lazyData != null)
                    _readyData = _lazyData.Value;
                else
                    throw new InvalidOperationException("GetData() had no data source");
            }

            return _readyData;
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
            return new BSAFile(Filename, Name, _settings, _readyData, _lazyData, IsCompressed, OriginalSize, Size);
        }

        public BSAFile DeepCopy(string newPath = null, string newName = null)
        {
            return new BSAFile(newPath ?? Path.GetDirectoryName(Filename), newName ?? Name, _settings, _readyData, _lazyData, IsCompressed, OriginalSize, Size);
        }
    }
}
