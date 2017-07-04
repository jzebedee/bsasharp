using System;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Linq;
using BSAsharp.Format;
using System.Text;

namespace BSAsharp
{
    internal class BsaReader : IDisposable
    {
        private BsaHeader _header;
        public BsaHeader Header
        {
            get { return _header; }
            protected set { _header = value; }
        }

        private readonly MemoryMappedFile _mmf;

        public BsaReader(MemoryMappedFile mmf)
        {
            _mmf = mmf;
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

        public IEnumerable<BsaFolder> Read()
        {
            using(var headerStream = _mmf.CreateViewAccessor(0, BsaHeader.Size, MemoryMappedFileAccess.Read))
                headerStream.Read(0, out _header);

            if (Header.Version != Bsa.FalloutVersion)
                throw new NotImplementedException("Unsupported BSA version");

            //BStringPrefixed = Header.ArchiveFlags.HasFlag(ArchiveFlags.BStringPrefixed);
            //DefaultCompressed = Header.ArchiveFlags.HasFlag(ArchiveFlags.Compressed);

            long offset = Header.Offset;
            var folderDict = ReadFolders(ref offset, Header.FolderCount);
            var fileNames = ReadFileNameBlocks(offset, Header.FileCount);

            return BuildBsaLayout(folderDict, fileNames);
        }

        protected IEnumerable<BsaFolder> BuildBsaLayout(Dictionary<string, IList<FileRecord>> folderDict, IList<string> fileNames)
        {
            var pathedFiles = folderDict
                .SelectMany(kvp => kvp.Value.Select(record => new { path = kvp.Key, record }))
                .Zip(fileNames, (a, filename) => new { a.path, filename, a.record });

            var fileLookup = pathedFiles.ToLookup(a => a.path, a => new { a.filename, a.record });
            return
                from g in fileLookup
                let bsaFiles = g.Select(a => new BethesdaFile(g.Key, a.filename, a.record, _mmf.CreateViewStream(a.record.offset, a.record.size, MemoryMappedFileAccess.Read), Header.ArchiveFlags))
                select new BsaFolder(g.Key, bsaFiles);
        }

        protected Dictionary<string, IList<FileRecord>> ReadFolders(ref long offset, uint folderCount)
        {
            var folderDict = new Dictionary<string, IList<FileRecord>>();

            var folders = ReadFolderRecords(ref offset, folderCount);
            foreach (var folder in folders)
            {
                if (offset != folder.offset - Header.TotalFileNameLength)
                    throw new InvalidOperationException("Running offset did not match expected folder offset");

                var fileRecords = ReadFileRecordBlocks(ref offset, folder.count, out string folderName);
                folderDict.Add(folderName, fileRecords);
            }

            return folderDict;
        }

        protected FolderRecord[] ReadFolderRecords(ref long offset, uint folderCount)
        {
            var folders = new FolderRecord[folderCount];
            var folderRecordsSize = folderCount * FolderRecord.Size;

            using (var folderRecordView = _mmf.CreateViewAccessor(offset, folderRecordsSize, MemoryMappedFileAccess.Read))
            {
                var foldersRead = folderRecordView.ReadArray(0, folders, 0, (int)folderCount);
                if (foldersRead != folderCount)
                    throw new InvalidOperationException("Folder records read did not match folder record count");

                offset += folderRecordsSize;
                return folders;
            }
        }

        protected FileRecord[] ReadFileRecords(ref long offset, uint fileCount)
        {
            var files = new FileRecord[fileCount];
            var fileRecordsSize = fileCount * FileRecord.Size;

            using (var fileRecordView = _mmf.CreateViewAccessor(offset, fileRecordsSize, MemoryMappedFileAccess.Read))
            {
                var filesRead = fileRecordView.ReadArray(0, files, 0, (int)fileCount);
                if (filesRead != fileCount)
                    throw new InvalidOperationException("File records read did not match file record count");

                offset += fileRecordsSize;
                return files;
            }
        }

        protected FileRecord[] ReadFileRecordBlocks(ref long offset, uint fileCount, out string folderName)
        {
            using (var fileRecordBlockView = _mmf.CreateViewAccessor(offset, 0, MemoryMappedFileAccess.Read))
            {
                var bstringLen = fileRecordBlockView.ReadByte(0);

                var folderNameBytes = new byte[bstringLen];
                var bytesRead = fileRecordBlockView.ReadArray(sizeof(byte), folderNameBytes, 0, bstringLen);
                if (bytesRead != bstringLen)
                    throw new InvalidOperationException("Folder name read did not match length prefix");

                offset += sizeof(byte) + bstringLen;
                //trim trailing null
                folderName = Encoding.Default.GetString(folderNameBytes, 0, folderNameBytes.Length - 1);
            }

            return ReadFileRecords(ref offset, fileCount);
        }

        protected string[] ReadFileNameBlocks(long offset, uint count)
        {
            var fileNames = new string[(int)count];

            var fileNamesBytes = new byte[Header.TotalFileNameLength];
            using(var fileNamesView = _mmf.CreateViewAccessor(offset, Header.TotalFileNameLength, MemoryMappedFileAccess.Read))
            {
                var bytesRead = fileNamesView.ReadArray(0, fileNamesBytes, 0, fileNamesBytes.Length);
                if (bytesRead != Header.TotalFileNameLength)
                    throw new InvalidOperationException("File names read did not match total file name length");

                offset += bytesRead;
            }

            for (int i = 0, j = 0, last = 0; i < fileNamesBytes.Length && j < fileNames.Length; i++)
            {
                if(fileNamesBytes[i] == '\0')
                {
                    fileNames[j++] = Encoding.Default.GetString(fileNamesBytes, last, i-last);
                    last = i+1;
                }
            }

            return fileNames;
        }
    }
}
