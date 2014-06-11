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

        protected ArchiveSettings Settings { get; private set; }

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

        public IEnumerable<BSAFolder> Read()
        {
            Header = ReadHeader();
            long offset = Header.offset;

            var BStringPrefixed = Header.archiveFlags.HasFlag(ArchiveFlags.BStringPrefixed);
            var DefaultCompressed = Header.archiveFlags.HasFlag(ArchiveFlags.Compressed);
            Settings = new ArchiveSettings(DefaultCompressed, BStringPrefixed);

            var folderDict = ReadFolders(ref offset, Header.folderCount);
            var fileNames = ReadFileNameBlocks(ref offset, Header.fileCount);

            return BuildBSALayout(folderDict, fileNames);
        }

        protected IEnumerable<BSAFolder> BuildBSALayout(Dictionary<string, List<FileRecord>> folderDict, List<string> fileNames)
        {
            var pathedFiles = folderDict
                .SelectMany(kvp =>
                    kvp.Value.Select(record => new { path = kvp.Key, record }))
                .Zip(fileNames, (a, fn) => Tuple.Create(a.path, fn, a.record));
            var fileLookup = pathedFiles.ToLookup(tup => tup.Item1, tup => Tuple.Create(tup.Item2, tup.Item3));

            return
                from g in fileLookup
                let bsaFiles = g.Select(tup => new BSAFile(g.Key, tup.Item1, Settings, tup.Item2, ReaderFromMMF<byte>(tup.Item2.offset, tup.Item2.size)))
                select new BSAFolder(g.Key, bsaFiles);
        }

        protected BSAHeader ReadHeader()
        {
            using (var Reader = ReaderFromMMF<BSAHeader>(0))
                return Reader.ReadStruct<BSAHeader>();
        }

        protected Dictionary<string, List<FileRecord>> ReadFolders(ref long offset, uint folderCount)
        {
            var folderDict = new Dictionary<string, List<FileRecord>>();

            using (var frReader = ReaderFromMMF<FolderRecord>(offset, folderCount))
            {
                offset += folderCount * Marshal.SizeOf(typeof(FolderRecord));

                var folders = frReader.ReadBulkStruct<FolderRecord>((int)folderCount);
                foreach (var folder in folders)
                {
                    string name;
                    var fileRecords = ReadFileRecordBlocks(ref offset, folder.count, out name);

                    folderDict.Add(name, fileRecords);
                }
            }

            return folderDict;
        }

        protected List<FileRecord> ReadFileRecordBlocks(ref long offset, uint count, out string name)
        {
            long size = sizeof(byte); //name-length byte
            using (var stream = FromMMF(offset, size))
            {
                size += stream.ReadByte(); //folder name length
            }

            size += count * Marshal.SizeOf(typeof(FileRecord));
            using (var frbReader = new BinaryReader(FromMMF(offset, size)))
            {
                offset += size;

                name = frbReader.ReadBString(true);
                return frbReader.ReadBulkStruct<FileRecord>((int)count).ToList();
            }
        }

        protected List<string> ReadFileNameBlocks(ref long offset, uint count)
        {
            using (var Reader = new BinaryReader(FromMMF(offset, Header.totalFileNameLength)))
            {
                var fileNames = new List<string>((int)count);

                for (int i = 0; i < count; i++)
                    fileNames.Add(Reader.ReadCString());
                offset += Header.totalFileNameLength;

                return fileNames;
            }
        }
    }
}