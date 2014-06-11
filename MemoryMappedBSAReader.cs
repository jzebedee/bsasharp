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
        public const long TWO_GB_SIZE = 2147483648; //2^31

        protected bool DefaultCompressed { get; set; }
        //private bool BStringPrefixed { get; set; }

        public BSAHeader Header { get; protected set; }

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
            long offset = BSAWrapper.HEADER_OFFSET;

            //BStringPrefixed = header.archiveFlags.HasFlag(ArchiveFlags.BStringPrefixed);
            DefaultCompressed = Header.archiveFlags.HasFlag(ArchiveFlags.Compressed);
            Trace.Assert(!Header.archiveFlags.HasFlag(ArchiveFlags.BStringPrefixed));

            var kvpList = ReadFolders(ref offset, Header.folderCount);
            var fileNames = ReadFileNameBlocks(ref offset, Header.fileCount);

            return BuildBSALayout(kvpList, fileNames);
        }

        protected IEnumerable<BSAFolder> BuildBSALayout(List<KeyValuePair<string, FileRecord>> kvpList, List<string> fileNames)
        {
            var pathedFiles = kvpList.Zip(fileNames, (kvp, fn) => Tuple.Create(kvp.Key, fn, kvp.Value));
            var fileLookup = pathedFiles.ToLookup(tup => tup.Item1, tup => Tuple.Create(tup.Item2, tup.Item3));
            return
                from g in fileLookup
                select new BSAFolder(g.Key, g.Select(tup => new BSAFile(g.Key, tup.Item1, tup.Item2, ReaderFromMMF<byte>(tup.Item2.offset, tup.Item2.size), DefaultCompressed)));
        }

        protected BSAHeader ReadHeader()
        {
            using (var Reader = ReaderFromMMF<BSAHeader>(0))
                return Reader.ReadStruct<BSAHeader>();
        }

        protected List<KeyValuePair<string, FileRecord>> ReadFolders(ref long offset, uint folderCount)
        {
            var kvpList = new List<KeyValuePair<string, FileRecord>>();

            using (var frReader = ReaderFromMMF<FolderRecord>(offset, folderCount))
            {
                offset += folderCount * Marshal.SizeOf(typeof(FolderRecord));

                var folders = frReader.ReadBulkStruct<FolderRecord>((int)folderCount);
                foreach (var folder in folders)
                {
                    string name;
                    var fileRecords = ReadFileRecordBlocks(ref offset, folder.count, out name);

                    kvpList.AddRange(fileRecords.Select(record => new KeyValuePair<string, FileRecord>(name, record)));
                }
            }

            return kvpList;
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