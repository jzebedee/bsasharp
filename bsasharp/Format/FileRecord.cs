using System.Runtime.InteropServices;

namespace BSAsharp.Format
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct FileRecord
    {
        public ulong hash;
        public uint size;
        public uint offset;
    }
}