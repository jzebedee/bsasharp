using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BSAsharp
{
    public class BSAReader
    {
        private readonly Stream _bsa;
        public BSAReader(Stream bsaStream)
        {
            this._bsa = bsaStream;
        }

        public void Shit()
        {
            using (var br = new BinaryReader(_bsa))
            {
                var field = br.ReadChars(4);
                var version = br.ReadUInt32();
                var offset = br.ReadUInt32();
                var archiveFlags = br.ReadUInt32();
                var folderCount = br.ReadUInt32();
                var fileCount = br.ReadUInt32();
                var totalFolderNameLength = br.ReadUInt32();
                var totalFileNameLength = br.ReadUInt32();
                var fileFlags = br.ReadUInt32();
            }
        }
    }
}
