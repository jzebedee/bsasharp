using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace BSAsharp.Extensions
{
    public static class Util
    {
        public static ulong CreateHash(string fname, string ext)
        {
            ulong hash1 = (ulong)(fname[fname.Length - 1] | ((fname.Length > 2 ? fname[fname.Length - 2] : 0) << 8) | fname.Length << 16 | fname[0] << 24);

            switch (ext)
            {
                case ".kf":
                    hash1 |= 0x80;
                    break;
                case ".nif":
                    hash1 |= 0x8000;
                    break;
                case ".dds":
                    hash1 |= 0x8080;
                    break;
                case ".wav":
                    hash1 |= 0x80000000;
                    break;
            }

            ulong hash2 = 0, hash3 = 0;
            if (fname.Length > 3)
                for (int i = 1; i < fname.Length - 2; i++)
                    hash2 = (hash2 * 0x1003F) + fname[i];

            for (int i = 0; i < ext.Length; i++)
                hash3 = (hash3 * 0x1003F) + ext[i];

            hash2 = ((hash2 << 32) + (hash3 << 32));

            return hash2 + hash1;
        }
    }
}
