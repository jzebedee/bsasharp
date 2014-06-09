using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace BSAsharp.Format
{
    [StructLayout(LayoutKind.Sequential)]
    class FileRecord
    {
        public ulong hash;
        public uint size;
        public uint offset;
    }
}
