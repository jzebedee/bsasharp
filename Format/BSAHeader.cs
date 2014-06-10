using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace BSAsharp.Format
{
    //var field = _reader.ReadChars(4);
    //var version = _reader.ReadUInt32();
    //var offset = _reader.ReadUInt32();
    //var archiveFlags = (ArchiveFlags)_reader.ReadUInt32();
    //var folderCount = _reader.ReadUInt32();
    //var fileCount = _reader.ReadUInt32();
    //var totalFolderNameLength = _reader.ReadUInt32();
    //var totalFileNameLength = _reader.ReadUInt32();
    //var fileFlags = (FileFlags)_reader.ReadUInt32();

    [StructLayout(LayoutKind.Sequential)]
    public class BSAHeader
    {
        [MarshalAs(UnmanagedType.ByValArray, /*ArraySubType = UnmanagedType.U1,*/ SizeConst = 4)]
        public char[] field;
        public uint version;
        public uint offset;
        public ArchiveFlags archiveFlags;
        public uint folderCount;
        public uint fileCount;
        public uint totalFolderNameLength;
        public uint totalFileNameLength;
        public FileFlags fileFlags;
    }
}