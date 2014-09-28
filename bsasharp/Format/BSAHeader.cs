﻿using System.IO;

namespace BSAsharp.Format
{
    internal struct BSAHeader
    {
        internal const int Size = 0x24;

        internal BSAHeader(BinaryReader reader)
        {
            field = reader.ReadUInt32();
            version = reader.ReadUInt32();
            offset = reader.ReadUInt32();
            archiveFlags = (ArchiveFlags)reader.ReadUInt32();
            folderCount = reader.ReadUInt32();
            fileCount = reader.ReadUInt32();
            totalFolderNameLength = reader.ReadUInt32();
            totalFileNameLength = reader.ReadUInt32();
            fileFlags = (FileFlags)reader.ReadUInt32();
        }
        internal void Write(BinaryWriter writer)
        {
            writer.Write(field);
            writer.Write(version);
            writer.Write(offset);
            writer.Write((uint)archiveFlags);
            writer.Write(folderCount);
            writer.Write(fileCount);
            writer.Write(totalFolderNameLength);
            writer.Write(totalFileNameLength);
            writer.Write((uint)fileFlags);
        }

        public uint field;
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