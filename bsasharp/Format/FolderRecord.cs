using System.Runtime.InteropServices;

namespace BSAsharp.Format
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct FolderRecord
    {
        public ulong hash;
        public uint count;
        public uint offset;
    }
}