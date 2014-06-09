using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BSAsharp.Format
{
    [Flags]
    enum ArchiveFlags
    {
        NamedDirectories = 0x1,
        NamedFiles = 0x2,
        Compressed = 0x4,
        unk1 = 0x8,
        unk2 = 0x10,
        //0x20
        //0x40
        //0x80
        unk3 = 0x100,
        BStringPrefixed = 0x200,
        unk5 = 0x400,
        unk6 = 0x800
    }
}
