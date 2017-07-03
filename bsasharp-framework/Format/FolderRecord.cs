using System.IO;

namespace BSAsharp.Format
{
    internal struct FolderRecord
    {
        internal const int Size = 0x10;

        internal FolderRecord(BinaryReader reader)
        {
            hash = reader.ReadUInt64();
            count = reader.ReadUInt32();
            offset = reader.ReadUInt32();
        }
        internal void Write(BinaryWriter writer)
        {
            writer.Write(hash);
            writer.Write(count);
            writer.Write(offset);
        }

        public ulong hash;
        public uint count;
        public uint offset;
    }
}