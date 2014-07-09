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
    internal class BSAReader : IDisposable
    {
        public BSAHeader Header { get; protected set; }

        public ArchiveSettings Settings { get; private set; }

        private readonly MemoryMappedFile _mmf;

        public BSAReader(MemoryMappedFile mmf, CompressionOptions options)
        {
            this._mmf = mmf;
            this.Settings = new ArchiveSettings() { Options = options };
        }
        ~BSAReader()
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

        private uint GetBStringOffset(long offset)
        {
            using (var lengthReader = _mmf.ToReader<byte>(offset))
            {
                return lengthReader.ReadByte() + 1u;
            }
        }

        public IEnumerable<BSAFolder> Read()
        {
            using (var reader = _mmf.ToReader<BSAHeader>(0))
            {
                Header = new BSAHeader(reader);
            }

            if (Header.version != BSAWrapper.FALLOUT_VERSION)
                throw new NotImplementedException("Unsupported BSA version");

            Settings.BStringPrefixed = Header.archiveFlags.HasFlag(ArchiveFlags.BStringPrefixed);
            Settings.DefaultCompressed = Header.archiveFlags.HasFlag(ArchiveFlags.Compressed);

            long offset = BSAWrapper.HEADER_OFFSET;
            var folderDict = ReadFolders(ref offset, Header.folderCount);
            var fileNames = ReadFileNameBlocks(offset, Header.fileCount);

            return BuildBSALayout(folderDict, fileNames);
        }

        protected IEnumerable<BSAFolder> BuildBSALayout(Dictionary<string, IList<FileRecord>> folderDict, IList<string> fileNames)
        {
            var pathedFiles = folderDict
                .SelectMany(kvp =>
                    kvp.Value.Select(record => new { path = kvp.Key, record }))
                .Zip(fileNames, (a, fn) => new { a.path, fn, a.record });

            var fileLookup = pathedFiles.ToLookup(a => a.path, a => new { a.fn, a.record });
            return
                from g in fileLookup
                let bsaFiles =
                    g.Select(a =>
                        new BSAFile(
                            g.Key,
                            a.fn,
                            Settings,
                            a.record,
                            (off, len) => _mmf.ToStream(a.record.offset + off, len)))
                select new BSAFolder(g.Key, bsaFiles);
        }

        protected Dictionary<string, IList<FileRecord>> ReadFolders(ref long offset, uint folderCount)
        {
            var folderDict = new Dictionary<string, IList<FileRecord>>();

            using (var reader = _mmf.ToReaderBulk<FolderRecord>(offset, folderCount))
            {
                var folders = new List<FolderRecord>((int)folderCount);
                for (int i = 0; i < folderCount; i++)
                    folders.Add(new FolderRecord(reader));
                foreach (var folder in folders)
                {
                    offset = folder.offset - Header.totalFileNameLength;

                    string folderName;
                    var fileRecords = ReadFileRecordBlocks(ref offset, folder.count, out folderName);

                    folderDict.Add(folderName, fileRecords);
                }
            }

            return folderDict;
        }

        protected IList<FileRecord> ReadFileRecordBlocks(ref long offset, uint count, out string folderName)
        {
            var bstringLen = GetBStringOffset(offset);
            using (var nameReader = _mmf.ToReader(offset, bstringLen))
                folderName = nameReader.ReadBString(true);
            offset += bstringLen;

            var files = new List<FileRecord>((int)count);
            using (var reader = _mmf.ToReaderBulk<FileRecord>(offset, count))
            {
                offset += BSAWrapper.SIZE_RECORD * count;
                for (int i = 0; i < count; i++)
                    files.Add(new FileRecord(reader));
                return files;
            }
        }

        protected IList<string> ReadFileNameBlocks(long offset, uint count)
        {
            var fileNames = new List<string>((int)count);

            using (var reader = _mmf.ToReader(offset, Header.totalFileNameLength))
                for (int i = 0; i < count; i++)
                    fileNames.Add(reader.ReadCString());

            return fileNames;
        }
    }
}
