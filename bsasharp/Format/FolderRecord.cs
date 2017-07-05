using System.IO;

namespace BSAsharp.Format
{
    internal struct FolderRecord
    {
        public ulong hash;
        public uint count;
        public uint offset;
    }
}