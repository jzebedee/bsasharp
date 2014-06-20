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
        //0h
        [MarshalAs(UnmanagedType.ByValArray, /*ArraySubType = UnmanagedType.U1,*/ SizeConst = 4)]
        public char[] field;
        //4h
        public uint version;
        //8h
        public uint offset;
        //Ch
        public ArchiveFlags archiveFlags;
        //10h
        public uint folderCount;
        //14h
        public uint fileCount;
        //18h
        public uint totalFolderNameLength;
        //1Ch
        public uint totalFileNameLength;
        //20h
        public FileFlags fileFlags;
    }
}