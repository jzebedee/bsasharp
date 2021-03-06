﻿using System;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Linq;
using BSAsharp.Format;
using BSAsharp.Extensions;

namespace BSAsharp
{
    internal class BsaReader : IDisposable
    {
        public BsaHeader Header { get; protected set; }

        public ArchiveSettings Settings { get; private set; }

        private readonly MemoryMappedFile _mmf;

        public BsaReader(MemoryMappedFile mmf, CompressionOptions options)
        {
            _mmf = mmf;
            Settings = new ArchiveSettings { Options = options };
        }
        ~BsaReader()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_mmf != null)
                    _mmf.Dispose();
            }
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private byte ReadByte(long offset)
        {
            using (var reader = _mmf.ToReader(offset, sizeof(byte)))
                return reader.ReadByte();
        }

        public IEnumerable<BsaFolder> Read()
        {
            using (var reader = _mmf.ToReader(0, BsaHeader.Size))
                Header = new BsaHeader(reader);

            if (Header.Version != Bsa.FalloutVersion)
                throw new NotImplementedException("Unsupported BSA version");

            Settings.BStringPrefixed = Header.ArchiveFlags.HasFlag(ArchiveFlags.BStringPrefixed);
            Settings.DefaultCompressed = Header.ArchiveFlags.HasFlag(ArchiveFlags.Compressed);

            long offset = BsaHeader.Size;
            var folderDict = ReadFolders(ref offset, Header.FolderCount);
            var fileNames = ReadFileNameBlocks(offset, Header.FileCount);

            return BuildBsaLayout(folderDict, fileNames);
        }

        protected IEnumerable<BsaFolder> BuildBsaLayout(Dictionary<string, IList<FileRecord>> folderDict, IList<string> fileNames)
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
                        new BsaFile(
                            g.Key,
                            a.fn,
                            Settings,
                            a.record,
                            (off, len) => _mmf.ToReader(a.record.offset + off, len)))
                select new BsaFolder(g.Key, bsaFiles);
        }

        protected Dictionary<string, IList<FileRecord>> ReadFolders(ref long offset, uint folderCount)
        {
            var folderDict = new Dictionary<string, IList<FileRecord>>();

            using (var reader = _mmf.ToReader(offset, folderCount * FolderRecord.Size))
            {
                var folders = new List<FolderRecord>((int)folderCount);
                for (int i = 0; i < folderCount; i++)
                    folders.Add(new FolderRecord(reader));
                foreach (var folder in folders)
                {
                    offset = folder.offset - Header.TotalFileNameLength;

                    string folderName;
                    var fileRecords = ReadFileRecordBlocks(ref offset, folder.count, out folderName);

                    folderDict.Add(folderName, fileRecords);
                }
            }

            return folderDict;
        }

        protected IList<FileRecord> ReadFileRecordBlocks(ref long offset, uint count, out string folderName)
        {
            var bstringLen = ReadByte(offset++);
            using (var nameReader = _mmf.ToReader(offset, bstringLen))
                folderName = nameReader.ReadBString(bstringLen, true);
            offset += bstringLen;

            var files = new List<FileRecord>((int)count);
            using (var reader = _mmf.ToReader(offset, count * FileRecord.Size))
            {
                offset += count * FileRecord.Size;

                for (int i = 0; i < count; i++)
                    files.Add(new FileRecord(reader));

                return files;
            }
        }

        protected IList<string> ReadFileNameBlocks(long offset, uint count)
        {
            var fileNames = new List<string>((int)count);

            using (var reader = _mmf.ToReader(offset, Header.TotalFileNameLength))
                for (int i = 0; i < count; i++)
                    fileNames.Add(reader.ReadCString());

            return fileNames;
        }
    }
}
