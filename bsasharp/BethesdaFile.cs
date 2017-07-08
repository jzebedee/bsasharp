using System;
using System.IO;
using System.Diagnostics;
using BSAsharp.Format;
using BSAsharp.Extensions;
using BSAsharp.Compression;

namespace BSAsharp
{
    /// <summary>
    /// A managed representation of a BSA file record and its contents. BSAFile is not guaranteed to be valid after the BSAReader that created it is disposed.
    /// </summary>
    [DebuggerDisplay("{Filename}")]
    public class BethesdaFile : IBsaEntry
    {
        internal const uint FlagCompress = 1 << 30;
        
        internal static uint CalculateDataSize(string path, string name, uint size, ArchiveFlags flags)
        {
            name = name.ToLowerInvariant();
            path = Util.FixPath(path);
            var filename = System.IO.Path.Combine(path, name);

            uint ret = 0;

            var bstringPrefixed = flags.HasFlag(ArchiveFlags.BStringPrefixed);
            if (bstringPrefixed)
            {
                ret += (uint)filename.Length + sizeof(byte); //length byte
            }

            var defaultCompressed = flags.HasFlag(ArchiveFlags.DefaultCompressed);
            var isCompressFlagSet = (size & FlagCompress) != 0;
            var isCompressed = defaultCompressed ? !isCompressFlagSet : isCompressFlagSet;
            if (isCompressed)
            {
                ret += size & ~FlagCompress;
            }
            else
            {
                ret += size;
            }

            return ret;
        }

        public string Name { get; private set; }
        public string Filename { get; private set; }
        public string Path { get; private set; }

        /// <summary>
        /// <para>This flag controls whether the file will be compressed, depending on the setting of the archive's DefaultCompress flag.</para>
        /// <list>
        ///     <item>When true and DefaultCompress is true: file = NOT COMPRESSED,</item>
        ///     <item>When true and DefaultCompress is false: file = COMPRESSED,</item>
        ///     <item>When false and DefaultCompress is true: file = COMPRESSED,</item>
        ///     <item>When false and DefaultCompress is false: file = NOT COMPRESSED,</item>
        /// </list>
        /// </summary>
        public bool IsCompressFlagSet { get; set; }
        public byte[] Data { get; }

        //hash MUST be immutable due to undefined behavior when the sort changes in a SortedSet<T>
        public ulong Hash { get; }

        internal BethesdaFile(string path, string name, FileRecord record, Stream dataStream, ArchiveFlags flags) : this(path, name)
        {
            var reader = new BinaryReader(dataStream);

            var bstringPrefixed = flags.HasFlag(ArchiveFlags.BStringPrefixed);
            if (bstringPrefixed)
            {
                var prefixed_name = reader.ReadBString();
                Debug.Assert(prefixed_name == Filename);
            }

            var defaultCompressed = flags.HasFlag(ArchiveFlags.DefaultCompressed);
            IsCompressFlagSet = (record.size & FlagCompress) != 0;
            var isCompressed = defaultCompressed ? !IsCompressFlagSet : IsCompressFlagSet;
            if (isCompressed)
            {
                var originalSize = reader.ReadUInt32();

                var zlib = new Zlib();
                var inflatedData = zlib.Decompress(reader.BaseStream);
                if (inflatedData.Length != originalSize)
                    throw new InvalidOperationException("Inflated file data did not match original size");

                Data = inflatedData;
            }
            else
            {
                Data = reader.ReadBytes((int)record.size);
            }
        }
        public BethesdaFile(string path, string name, byte[] data) : this(path, name)
        {
            Data = data;
        }
        public BethesdaFile(string path, string name)
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
    }
}
