using System;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Linq;
using BSAsharp.Format;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;
using BSAsharp.Extensions;
using System.Diagnostics;

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

        private readonly Stream _stream;

        public BsaReader(MemoryMappedFile mmf)
        {
            _stream = mmf.CreateViewStream(0, 0, MemoryMappedFileAccess.Read);
        }
        //~BsaReader()
        //{
        //    Dispose(false);
        //}

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _stream?.Dispose();
            }
        }
        public void Dispose()
        {
            Dispose(true);
            //GC.SuppressFinalize(this);
        }

        public IEnumerable<BsaFolder> Read()
        {
            return Read(_stream);
        }

        protected IEnumerable<BsaFolder> Read(Stream stream)
        {
            using (var reader = new BinaryReader(stream, Encoding.Default, true))
            {
                _header = reader.Read<BsaHeader>();

                if (Header.Version != Bsa.FalloutVersion)
                    throw new NotImplementedException("Unsupported BSA version");

                long offset = Header.Offset;
                var folderDict = ReadFolders(reader, ref offset, Header.FolderCount, Header.ArchiveFlags);
                var fileNames = ReadFileNameBlocks(reader, offset, Header.FileCount);

                return BuildBsaLayout(folderDict, fileNames);
            }
        }

        protected Stream GetBoundedStream(uint offset, uint length)
        {
            long startPosition = _stream.Position;

            var buffer = new byte[length];
            try
            {
                _stream.Seek(offset, SeekOrigin.Begin);
                if (_stream.Read(buffer, 0, buffer.Length) != buffer.Length)
                    throw new InvalidOperationException("Buffer read did not match provided length for bounded stream");
            }
            finally
            {
                _stream.Seek(startPosition, SeekOrigin.Begin);
            }

            return new MemoryStream(buffer);
        }

        protected IEnumerable<BsaFolder> BuildBsaLayout(Dictionary<string, IList<FileRecord>> folderDict, IList<string> fileNames)
        {
            var pathedFiles = folderDict
                .SelectMany(kvp => kvp.Value.Select(record => new { path = kvp.Key, record }))
                .Zip(fileNames, (a, filename) => new { a.path, filename, a.record });

            var fileLookup = pathedFiles.ToLookup(a => a.path, a => new { a.filename, a.record });
            return
                from files in fileLookup
                let bsaFiles =
                    from file in files
                    let dataLen = CalculateDataSize(files.Key, file.filename, file.record, Header.ArchiveFlags)
                    select new BethesdaFile(files.Key, file.filename, file.record, GetBoundedStream(file.record.offset, dataLen), Header.ArchiveFlags)
                select new BsaFolder(files.Key, bsaFiles);
        }

        private uint CalculateDataSize(string path, string name, FileRecord record, ArchiveFlags flags)
        {
            name = name.ToLowerInvariant();
            path = Util.FixPath(path);
            var filename = Path.Combine(path, name);

            uint ret = 0;

            var bstringPrefixed = flags.HasFlag(ArchiveFlags.BStringPrefixed);
            if (bstringPrefixed)
            {
                ret += (uint)filename.Length + sizeof(byte); //length byte
            }

            var defaultCompressed = flags.HasFlag(ArchiveFlags.DefaultCompressed);
            var isCompressFlagSet = (record.size & BethesdaFile.FlagCompress) != 0;
            var isCompressed = defaultCompressed ? !isCompressFlagSet : isCompressFlagSet;
            if (isCompressed)
            {
                ret += record.size & ~BethesdaFile.FlagCompress;
            }
            else
            {
                ret += record.size;
            }

            return ret;
        }

        protected Dictionary<string, IList<FileRecord>> ReadFolders(BinaryReader reader, ref long offset, uint folderCount, ArchiveFlags flags)
        {
            var folderDict = new Dictionary<string, IList<FileRecord>>();

            var namedDirectories = flags.HasFlag(ArchiveFlags.NamedDirectories);

            var folders = ReadFolderRecords(reader, ref offset, folderCount);
            foreach (var folder in folders)
            {
                if (offset != folder.offset - Header.TotalFileNameLength)
                    throw new InvalidOperationException("Running offset did not match expected folder offset");

                var fileRecords = ReadFileRecordBlocks(reader, ref offset, folder.count, namedDirectories, out string folderName);
                folderDict.Add(folderName, fileRecords);
            }

            return folderDict;
        }

        protected FolderRecord[] ReadFolderRecords(BinaryReader reader, ref long offset, uint folderCount)
        {
            offset += folderCount * Marshal.SizeOf(typeof(FolderRecord));
            return reader.ReadArray<FolderRecord>((int)folderCount);
        }

        protected FileRecord[] ReadFileRecords(BinaryReader reader, ref long offset, uint fileCount)
        {
            offset += fileCount * Marshal.SizeOf(typeof(FileRecord));
            return reader.ReadArray<FileRecord>((int)fileCount);
        }

        protected FileRecord[] ReadFileRecordBlocks(BinaryReader reader, ref long offset, uint fileCount, bool namedDirectories, out string folderName)
        {
            if (namedDirectories)
            {
                var startPos = reader.BaseStream.Position;
                folderName = reader.ReadBString(true);
                var endPos = reader.BaseStream.Position;
                offset += (endPos - startPos);
            }
            else
            {
                folderName = null;
            }

            return ReadFileRecords(reader, ref offset, fileCount);
        }

        protected string[] ReadFileNameBlocks(BinaryReader reader, long offset, uint count)
        {
            var fileNamesBytes = reader.ReadBytes((int)Header.TotalFileNameLength);
            offset += Header.TotalFileNameLength;

            var fileNames = new string[(int)count];
            for (int i = 0, j = 0, last = 0; i < fileNamesBytes.Length && j < fileNames.Length; i++)
            {
                if (fileNamesBytes[i] == '\0')
                {
                    fileNames[j++] = Encoding.Default.GetString(fileNamesBytes, last, i - last);
                    last = i + 1;
                }
            }

            return fileNames;
        }
    }
}
