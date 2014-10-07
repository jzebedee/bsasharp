using System;
using System.IO;
using System.Diagnostics;
using BSAsharp.Format;
using BSAsharp.Extensions;

namespace BSAsharp
{
    /// <summary>
    /// A managed representation of a BSA file record and its contents. BSAFile is not guaranteed to be valid after the BSAReader that created it is disposed.
    /// </summary>
    [DebuggerDisplay("{Filename}")]
    public class BsaFile : IBsaEntry, ICloneable
    {
        const uint FlagCompress = 1 << 30;
        const int DeflateLevelSize = 9, DeflateLevelSpeed = 1, DeflateLevelMixed = 5;

        public string Name { get; private set; }
        public string Filename { get; private set; }
        public string Path { get; private set; }

        public bool IsCompressed { get; private set; }

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
        public ulong Hash { get; private set; }

        private byte[] _readyData;
        private Lazy<byte[]> _lazyData;

        private readonly ArchiveSettings _settings;
        private bool _forceCompressionChecked;
        private int? _optDeflateLevel;

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

        public BsaFile(string path, string name, ArchiveSettings settings, byte[] data, bool inputCompressed)
            : this(path, name, settings)
        {
            //inputCompressed param specifies compression of data param, NOT final result!
            UpdateData(data, inputCompressed);
        }

        internal BsaFile(string path, string name, ArchiveSettings settings, FileRecord baseRec, Func<uint, uint, BinaryReader> createReader)
            : this(path, name, settings, baseRec)
        {
            if (Size == 0 || (Size <= 4 && IsCompressed))
            {
                _readyData = new byte[0];
                OriginalSize = 0;
            }

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

        private BsaFile(string path, string name, ArchiveSettings settings, FileRecord baseRec)
            : this(path, name, settings)
        {
            bool compressBitSet = (baseRec.size & FlagCompress) != 0;
            if (compressBitSet)
                baseRec.size ^= FlagCompress;

            IsCompressed = CheckCompressed(compressBitSet);
            Size = baseRec.size;
        }
        private BsaFile(string path, string name, ArchiveSettings settings)
            : this(path, name)
        {
            _settings = settings;

            CheckCompressionSettings();

            if (Strategy.HasFlag(CompressionStrategy.Aggressive))
                throw new NotImplementedException("CompressionStrategy.Aggressive");
        }

        //Clone ctor
        private BsaFile(string path, string name, ArchiveSettings settings, byte[] readyData, Lazy<byte[]> lazyData, bool isCompressed, uint originalSize, uint size)
            : this(path, name)
        {
            _settings = settings;

            _readyData = readyData;
            _lazyData = lazyData;

            IsCompressed = isCompressed;

            OriginalSize = originalSize;
            Size = size;

            CheckCompressionSettings();
        }
        private BsaFile(string path, string name)
        {
            Name = name.ToLowerInvariant();
            Path = Util.FixPath(path);
            Filename = System.IO.Path.Combine(Path, Name);

            Hash = Util.CreateHash(System.IO.Path.GetFileNameWithoutExtension(Name), System.IO.Path.GetExtension(Name));
        }

        public override string ToString()
        {
            return Filename;
        }

        public uint CalculateRecordSize()
        {
            ForceCompressionIfNeeded();

            uint baseSize = Size;

            bool setCompressBit = CheckCompressed(IsCompressed);
            if (setCompressBit)
                baseSize |= FlagCompress;

            return baseSize;
        }

        private void CheckCompressionSettings()
        {
            int extDeflateLevel;
            if (Options.ExtensionCompressionLevel.TryGetValue(System.IO.Path.GetExtension(Name), out extDeflateLevel))
            {
                _optDeflateLevel = extDeflateLevel;
                if (_optDeflateLevel < 0)
                    //blocks compression changing on this file
                    _forceCompressionChecked = true;
            }
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

            if (IsCompressed)
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

            return GetDeflatedData(force);
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
                return DeflateLevelMixed;
            if (Strategy.HasFlag(CompressionStrategy.Speed))
                return DeflateLevelSpeed;
            if (Strategy.HasFlag(CompressionStrategy.Size))
                return DeflateLevelSize;

            throw new ArgumentException("CompressionStrategy did not have enough information");
        }

        public object Clone()
        {
            return new BsaFile(Filename, Name, _settings, _readyData, _lazyData, IsCompressed, OriginalSize, Size);
        }

        public BsaFile DeepCopy(string newPath = null, string newName = null)
        {
            return new BsaFile(newPath ?? Path, newName ?? Name, _settings, _readyData, _lazyData, IsCompressed, OriginalSize, Size);
        }
    }
}
