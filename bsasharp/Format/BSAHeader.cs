using System.IO;

namespace BSAsharp.Format
{
    public struct BsaHeader
    {
        internal const int Size = 0x24;

        internal BsaHeader(BinaryReader reader)
        {
            Field = reader.ReadUInt32();
            Version = reader.ReadUInt32();
            Offset = reader.ReadUInt32();
            ArchiveFlags = (ArchiveFlags)reader.ReadUInt32();
            FolderCount = reader.ReadUInt32();
            FileCount = reader.ReadUInt32();
            TotalFolderNameLength = reader.ReadUInt32();
            TotalFileNameLength = reader.ReadUInt32();
            FileFlags = (FileFlags)reader.ReadUInt32();
        }
        internal void Write(BinaryWriter writer)
        {
            writer.Write(Field);
            writer.Write(Version);
            writer.Write(Offset);
            writer.Write((uint)ArchiveFlags);
            writer.Write(FolderCount);
            writer.Write(FileCount);
            writer.Write(TotalFolderNameLength);
            writer.Write(TotalFileNameLength);
            writer.Write((uint)FileFlags);
        }

        public uint Field;
        public uint Version;
        public uint Offset;
        public ArchiveFlags ArchiveFlags;
        public uint FolderCount;
        public uint FileCount;
        public uint TotalFolderNameLength;
        public uint TotalFileNameLength;
        public FileFlags FileFlags;
    }
}