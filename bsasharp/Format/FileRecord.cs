using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace BSAsharp.Format
{
    [StructLayout(LayoutKind.Sequential)]
    public class FileRecord
    {
        public FileRecord(BinaryReader reader)
        {
            hash = reader.ReadUInt64();
            size = reader.ReadUInt32();
            offset = reader.ReadUInt32();
        }
        public FileRecord() { }
        public void Write(BinaryWriter writer)
        {
            writer.Write(hash);
            writer.Write(size);
            writer.Write(offset);
        }

        public ulong hash;
        public uint size;
        public uint offset;
    }
}