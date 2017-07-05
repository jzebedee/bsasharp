using System.IO;

namespace BSAsharp.Format
{
    public struct BsaHeader
    {
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