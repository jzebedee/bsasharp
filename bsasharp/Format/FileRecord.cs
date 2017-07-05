using System.IO;

namespace BSAsharp.Format
{
    internal struct FileRecord
    {
        public ulong hash;
        public uint size;
        public uint offset;
    }
}