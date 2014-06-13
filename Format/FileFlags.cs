using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BSAsharp.Format
{
    [Flags]
    public enum FileFlags
    {
        Nif = 1 << 0, //meshes
        Dds = 1 << 1, //textures
        Xml = 1 << 2, //menus
        Wav = 1 << 3, //sounds
        Ogg = 1 << 4, //voices
        Txt = 1 << 5, //shaders
        Spt = 1 << 6, //trees
        Tex = 1 << 7, //fonts
        Ctl = 1 << 8, //misc?
        unk10 = 1 << 9,
        unk11 = 1 << 10,
        unk12 = 1 << 11,
        unk13 = 1 << 12,
        unk14 = 1 << 13,
        unk15 = 1 << 14,
        unk16 = 1 << 15,
        unk17 = 1 << 16,
        unk18 = 1 << 17,
        unk19 = 1 << 18,
        unk20 = 1 << 19,
        unk21 = 1 << 20,
        unk22 = 1 << 21,
        unk23 = 1 << 22,
        unk24 = 1 << 23,
        unk25 = 1 << 24,
        unk26 = 1 << 25,
        unk27 = 1 << 26,
        unk28 = 1 << 27,
        unk29 = 1 << 28,
        unk30 = 1 << 29,
        unk31 = 1 << 30,
    }
}