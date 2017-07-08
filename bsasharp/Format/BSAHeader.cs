using System.Runtime.InteropServices;

namespace BSAsharp.Format
{
    [StructLayout(LayoutKind.Sequential)]
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