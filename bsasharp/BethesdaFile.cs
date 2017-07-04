using System;
using System.IO;
using System.Diagnostics;
using BSAsharp.Format;
using BSAsharp.Extensions;
using System.IO.MemoryMappedFiles;

namespace BSAsharp
{
    /// <summary>
    /// A managed representation of a BSA file record and its contents. BSAFile is not guaranteed to be valid after the BSAReader that created it is disposed.
    /// </summary>
    [DebuggerDisplay("{Filename}")]
    public class BethesdaFile : IBsaEntry
    {
        const uint FlagCompress = 1 << 30;

        public string Name { get; private set; }
        public string Filename { get; private set; }
        public string Path { get; private set; }

        public bool ShouldCompress { get; private set; }
        public byte[] Data { get; }

        //hash MUST be immutable due to undefined behavior when the sort changes in a SortedSet<T>
        public ulong Hash { get; }

        internal BethesdaFile(string path, string name, FileRecord record, MemoryMappedViewStream dataStream, ArchiveFlags flags) : this(path, name)
        {
            var reader = new BinaryReader(dataStream);

            var bstringPrefixed = flags.HasFlag(ArchiveFlags.BStringPrefixed);
            if (bstringPrefixed)
            {
                var prefixed_name = reader.ReadBString();
                Debug.Assert(prefixed_name == Filename);
            }

            var size = record.size;

            var defaultCompressed = flags.HasFlag(ArchiveFlags.DefaultCompressed);
            var hasCompressFlag = (record.size & FlagCompress) != 0;
            var isCompressed = defaultCompressed ? !hasCompressFlag : hasCompressFlag;
            if (isCompressed)
            {
                var originalSize = reader.ReadUInt32();
                size -= sizeof(UInt32);

                var zlib = new Zlib();
                var inflatedData = zlib.Decompress(reader.BaseStream);
                if (inflatedData.Length != originalSize)
                    throw new InvalidOperationException("Inflated file data did not match original size");

                Data = inflatedData;
            }
            else
            {
                Data = reader.ReadBytes((int)size);
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
