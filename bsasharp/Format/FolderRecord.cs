using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace BSAsharp.Format
{
    [StructLayout(LayoutKind.Sequential)]
    public struct FolderRecord //0x10
    {
        public FolderRecord(BinaryReader reader)
        {
            hash = reader.ReadUInt64();
            count = reader.ReadUInt32();
            offset = reader.ReadUInt32();
        }
        public void Write(BinaryWriter writer)
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