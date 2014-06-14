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

        private BinaryReader _reader;

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

        public IEnumerable<BSAFolder> Read()
        {
            //using (
                _reader = ReaderFromMMF(0, 0);
                //)
            {
                Header = _reader.ReadStruct<BSAHeader>();

                var BStringPrefixed = Header.archiveFlags.HasFlag(ArchiveFlags.BStringPrefixed);
                var DefaultCompressed = Header.archiveFlags.HasFlag(ArchiveFlags.Compressed);
                Settings = new ArchiveSettings(DefaultCompressed, BStringPrefixed);

                var folderDict = ReadFolders(Header.folderCount);
                var fileNames = ReadFileNameBlocks(Header.fileCount);

                return BuildBSALayout(folderDict, fileNames);
            }
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
                let bsaFiles = g.Select(tup =>
                {
                    var path = g.Key;
                    var name = tup.Item1;
                    var fileRec = tup.Item2;

                    var fileOffset = tup.Item2.offset;

                    if (Settings.BStringPrefixed)
                        using (var lengthReader = ReaderFromMMF<byte>(fileOffset))
                        {
                            var bstringLen = lengthReader.ReadByte() + 1u;
                            if (bstringLen != 1)
                            {
                                fileOffset += bstringLen;
                                if (fileRec.size > bstringLen)
                                    fileRec.size -= bstringLen;
                            }
                        }

                    return new BSAFile(path, name, Settings, fileRec, () => ReaderFromMMF<byte>(fileOffset, fileRec.size));
                })
                select new BSAFolder(g.Key, bsaFiles);
        }

        protected Dictionary<string, IEnumerable<FileRecord>> ReadFolders(uint folderCount)
        {
            var folderDict = new Dictionary<string, IEnumerable<FileRecord>>();

            var folders = _reader.ReadBulkStruct<FolderRecord>((int)folderCount);
            foreach (var folder in folders)
            {
                var folderOffset = folder.offset - Header.totalFileNameLength;
                Trace.Assert(_reader.BaseStream.Position == folderOffset);

                string folderName;
                var fileRecords = ReadFileRecordBlocks(folder.count, out folderName);

                folderDict.Add(folderName, fileRecords);
            }

            return folderDict;
        }

        protected IEnumerable<FileRecord> ReadFileRecordBlocks(uint count, out string folderName)
        {
            folderName = _reader.ReadBString(true);
            return _reader.ReadBulkStruct<FileRecord>((int)count).ToList();
        }

        protected List<string> ReadFileNameBlocks(uint count)
        {
            var fileNames = new List<string>((int)count);

            for (int i = 0; i < count; i++)
                fileNames.Add(_reader.ReadCString());

            return fileNames;
        }
    }
}