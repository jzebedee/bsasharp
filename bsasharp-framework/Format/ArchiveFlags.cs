using System;

namespace BSAsharp.Format
{
    [Flags]
    public enum ArchiveFlags
    {
        NamedDirectories = 1 << 0,
        NamedFiles = 1 << 1,
        Compressed = 1 << 2,
        unk1 = 1 << 3,
        unk2 = 1 << 4,
        unk3 = 1 << 5,
        unk4 = 1 << 6,
        unk5 = 1 << 7,
        BStringPrefixed = 1 << 8,
        unk6 = 1 << 9,
        unk7 = 1 << 10
    }
}