using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;
using BSAsharp.Format;
using BSAsharp.Extensions;

namespace BSAsharp
{
    internal class MemoryMappedBSAReader : IDisposable
    {
        public BSAHeader Header { get; protected set; }

        public ArchiveSettings Settings { get; private set; }

        private readonly MemoryMappedFile _mmf;

        public MemoryMappedBSAReader(MemoryMappedFile mmf)
        {
            this._mmf = mmf;
        }
        ~MemoryMappedBSAReader()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _mmf.Dispose();
            }
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
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

        private BinaryReader ReaderFromMMF<T>(long offset)
        {
            return new BinaryReader(FromMMF<T>(offset));
        }

        private BinaryReader ReaderFromMMF<T>(long offset, uint count)
        {
            return new BinaryReader(FromMMF<T>(offset, count));
        }

        private BinaryReader ReaderFromMMF(long offset, long size)
        {
            return new BinaryReader(FromMMF(offset, size));
        }

        private uint GetBStringOffset(long offset)
        {
            using (var lengthReader = ReaderFromMMF<byte>(offset))
            {
                return lengthReader.ReadByte() + 1u;
            }
        }

        public IEnumerable<BSAFolder> Read()
        {
            using (var reader = ReaderFromMMF<BSAHeader>(0))
            {
                Header = reader.ReadStruct<BSAHeader>();
            }

            var BStringPrefixed = Header.archiveFlags.HasFlag(ArchiveFlags.BStringPrefixed);
            var DefaultCompressed = Header.archiveFlags.HasFlag(ArchiveFlags.Compressed);
            Settings = new ArchiveSettings(DefaultCompressed, BStringPrefixed);

            long offset = BSAWrapper.HEADER_OFFSET;
            var folderDict = ReadFolders(ref offset, Header.folderCount);
            var fileNames = ReadFileNameBlocks(offset, Header.fileCount);

            return BuildBSALayout(folderDict, fileNames);
        }

        protected IEnumerable<BSAFolder> BuildBSALayout(Dictionary<string, IEnumerable<FileRecord>> folderDict, List<string> fileNames)
        {
            var pathedFiles = folderDict
                .SelectMany(kvp =>
                    kvp.Value.Select(record => new { path = kvp.Key, record }))
                .Zip(fileNames, (a, fn) => Tuple.Create(a.path, fn, a.record));
            var fileLookup = pathedFiles.ToLookup(tup => tup.Item1, tup => Tuple.Create(tup.Item2, tup.Item3));

            return
                from g in fileLookup
                let bsaFiles = g.Select(tup => new BSAFile(g.Key, tup.Item1, Settings, tup.Item2, () => ReaderFromMMF<byte>(tup.Item2.offset, tup.Item2.size)))
                select new BSAFolder(g.Key, bsaFiles);
        }

        protected Dictionary<string, IEnumerable<FileRecord>> ReadFolders(ref long offset, uint folderCount)
        {
            var folderDict = new Dictionary<string, IEnumerable<FileRecord>>();

            using (var reader = ReaderFromMMF<FolderRecord>(offset, folderCount))
            {
                var folders = reader.ReadBulkStruct<FolderRecord>((int)folderCount);
                foreach (var folder in folders)
                {
                    //var folderOffset = folder.offset - Header.totalFileNameLength;
                    offset = folder.offset - Header.totalFileNameLength;

                    string folderName;
                    var fileRecords = ReadFileRecordBlocks(ref offset, folder.count, out folderName);

                    folderDict.Add(folderName, fileRecords);
                }
            }

            return folderDict;
        }

        protected IEnumerable<FileRecord> ReadFileRecordBlocks(ref long offset, uint count, out string folderName)
        {
            var bstringLen = GetBStringOffset(offset);
            using (var nameReader = ReaderFromMMF(offset, bstringLen))
                folderName = nameReader.ReadBString(true);
            offset += bstringLen;

            List<FileRecord> records;
            using (var recordReader = ReaderFromMMF<FileRecord>(offset, count))
                records = recordReader.ReadBulkStruct<FileRecord>((int)count).ToList();
            offset += BSAWrapper.SIZE_RECORD * count;

            return records;
        }

        protected List<string> ReadFileNameBlocks(long offset, uint count)
        {
            var fileNames = new List<string>((int)count);

            using (var reader = ReaderFromMMF(offset, Header.totalFileNameLength))
                for (int i = 0; i < count; i++)
                    fileNames.Add(reader.ReadCString());

            return fileNames;
        }
    }
}