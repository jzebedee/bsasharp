using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BSAsharp.Format
{
    [Flags]
    public enum FileFlags
    {
        Nif = 1 << 0,
        Dds = 1 << 1,
        Xml = 1 << 2,
        Wav = 1 << 3,
        Ogg = 1 << 4,
        Txt = 1 << 5,
        Spt = 1 << 6,
        Tex = 1 << 7,
        Ctl = 1 << 8,
    }
}