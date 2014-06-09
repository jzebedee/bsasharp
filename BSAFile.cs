using BSAsharp.Format;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BSAsharp
{
    class BSAFile
    {
        const uint FLAG_COMPRESS = 1 << 30;

        public static bool DefaultCompressed { get; set; }

        public string Name { get; private set; }
        public bool IsCompressed { get; private set; }

        private byte[] data;

        public BSAFile(string name, FileRecord baseRec, BinaryReader reader, bool resetStream = false)
        {
            this.Name = name;

            bool compressBitSet = (baseRec.size & FLAG_COMPRESS) != 0;
            this.IsCompressed = DefaultCompressed ^ compressBitSet;

            long streamPos = reader.BaseStream.Position;
            try
            {
                reader.BaseStream.Seek(baseRec.offset, SeekOrigin.Begin);
                data = reader.ReadBytes((int)baseRec.size);
            }
            finally
            {
                if (resetStream)
                    reader.BaseStream.Seek(streamPos, SeekOrigin.Begin);
            }
        }
    }
}
