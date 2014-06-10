using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BSAsharp.Format
{
    [Flags]
    public enum FileFlags
    {
        Nif = 0x1,
        Dds = 0x2,
        Xml = 0x4,
        Wav = 0x8,
        Snd = 0x10,
        Doc = 0x20,
        Spt = 0x40,
        Tex = 0x80,
        Ctl = 0x100,
    }
}