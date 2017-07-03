using System.IO;

namespace BSAsharp.Format
{
    internal struct FileRecord
    {
        internal const int Size = 0x10;

        internal FileRecord(BinaryReader reader)
        {
            hash = reader.ReadUInt64();
            size = reader.ReadUInt32();
            offset = reader.ReadUInt32();
        }
        internal void Write(BinaryWriter writer)
        {
            writer.Write(hash);
            writer.Write(size);
            writer.Write(offset);
        }

        public ulong hash;
        public uint size;
        public uint offset;
    }
}