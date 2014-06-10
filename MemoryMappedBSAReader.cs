using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using BSAsharp.Format;
using System.Runtime.InteropServices;

namespace BSAsharp
{
    public class MemoryMappedBSAReader : BSAReader
    {
        const long TWO_GB_LIMIT = 2147483648; //2^31

        private readonly MemoryMappedFile _mmf;
        private BSAHeader _header;

        private long _offset = 0;
        private long _size;

        /// <summary>
        /// Initializes a memory-mapped reader for a BSA
        /// </summary>
        /// <param name="bsaStream">A path to the BSA file to read</param>
        /// <param name="size">The size of the BSA in bytes</param>
        public MemoryMappedBSAReader(string bsaPath, long size)
        {
            if (size > TWO_GB_LIMIT)
                throw new ArgumentException("BSA cannot be greater than 2GiB");

            this._size = size;
            this._mmf = MemoryMappedFile.CreateFromFile(bsaPath, FileMode.Open, Path.GetFileName(Path.GetTempFileName()), size, MemoryMappedFileAccess.Read);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _mmf.Dispose();
            }

            base.Dispose(disposing);
        }

        private Stream FromMMF(long offset, long size)
        {
            return _mmf.CreateViewStream(offset, size, MemoryMappedFileAccess.Read);
        }

        private Stream FromMMF<T>(long offset)
        {
            return FromMMF(offset, Marshal.SizeOf(typeof(T)));
        }

        private Stream FromMMF<T>(long offset, uint count)
        {
            return FromMMF(offset, Marshal.SizeOf(typeof(T)) * count);
        }

        private BinaryReader ReaderFromMMF<T>(long offset = -1)
        {
            return new BinaryReader(FromMMF<T>(offset < 0 ? _offset : offset));
        }

        private BinaryReader ReaderFromMMF<T>(uint count, long offset = -1)
        {
            return new BinaryReader(FromMMF<T>(offset < 0 ? _offset : offset, count));
        }

        protected override IEnumerable<BSAFolder> BuildBSALayout(List<KeyValuePair<string, FileRecord>> kvpList, List<string> fileNames)
        {
            var pathedFiles = kvpList.Zip(fileNames, (kvp, fn) => Tuple.Create(kvp.Key, fn, kvp.Value));
            var fileLookup = pathedFiles.ToLookup(tup => tup.Item1, tup => Tuple.Create(tup.Item2, tup.Item3));
            return
                from g in fileLookup
                select new BSAFolder(g.Key, g.Select(tup => new BSAFile(g.Key, tup.Item1, tup.Item2, ReaderFromMMF<byte>(tup.Item2.size, tup.Item2.offset), false, false)));
        }

        protected override BSAHeader ReadHeader()
        {
            using (Reader = ReaderFromMMF<BSAHeader>())
                try
                {
                    return (_header = base.ReadHeader());
                }
                finally
                {
                    _offset += Reader.BaseStream.Position;
                }
        }

        protected override List<FolderRecord> ReadFolderRecord(uint count)
        {
            using (Reader = ReaderFromMMF<FolderRecord>(count))
                try
                {
                    return base.ReadFolderRecord(count);
                }
                finally
                {
                    _offset += Reader.BaseStream.Position;
                }
        }

        protected override List<FileRecord> ReadFileRecordBlocks(uint count, out string name)
        {
            int size = 1; //name-length byte
            using (var stream = FromMMF(_offset, 0xFF))
            {
                size += stream.ReadByte(); //folder name length
            }
            using (Reader = new BinaryReader(FromMMF(_offset, size + (count * Marshal.SizeOf(typeof(FileRecord))))))
                try
                {
                    return base.ReadFileRecordBlocks(count, out name);
                }
                finally
                {
                    _offset += Reader.BaseStream.Position;
                }
        }

        protected override List<string> ReadFileNameBlocks(uint count)
        {
            using (Reader = new BinaryReader(FromMMF(_offset, _header.totalFileNameLength)))
                try
                {
                    return base.ReadFileNameBlocks(count);
                }
                finally
                {
                    _offset += Reader.BaseStream.Position;
                }
        }
    }
}