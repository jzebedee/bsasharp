using BSAsharp.Format;
using BSAsharp.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BSAsharp
{
    internal class BSAReader : IDisposable
    {
        protected BinaryReader Reader { get; set; }

        protected bool DefaultCompressed { get; private set; }
        //private bool BStringPrefixed { get; set; }

        public BSAHeader Header { get; protected set; }

        /// <summary>
        /// Initializes a standard I/O reader for a BSA
        /// </summary>
        /// <param name="bsaStream">A stream that represents the BSA to read</param>
        public BSAReader(Stream bsaStream)
        {
            this.Reader = new BinaryReader(bsaStream);
        }
        protected BSAReader()
        {

        }
        ~BSAReader()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Reader.Dispose();
            }
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public virtual IEnumerable<BSAFolder> Read()
        {
            Header = ReadHeader();
            var kvpList = ReadFolders(Header.folderCount);

            DefaultCompressed = Header.archiveFlags.HasFlag(ArchiveFlags.Compressed);
            Trace.Assert(!Header.archiveFlags.HasFlag(ArchiveFlags.BStringPrefixed));
            //BStringPrefixed = header.archiveFlags.HasFlag(ArchiveFlags.BStringPrefixed);

            var fileNames = ReadFileNameBlocks(Header.fileCount);

            return BuildBSALayout(kvpList, fileNames);
        }

        protected virtual IEnumerable<BSAFolder> BuildBSALayout(List<KeyValuePair<string, FileRecord>> kvpList, List<string> fileNames)
        {
            var pathedFiles = kvpList.Zip(fileNames, (kvp, fn) => Tuple.Create(kvp.Key, fn, kvp.Value));
            var fileLookup = pathedFiles.ToLookup(tup => tup.Item1, tup => Tuple.Create(tup.Item2, tup.Item3));
            return
                from g in fileLookup
                select new BSAFolder(g.Key, g.Select(tup => new BSAFile(g.Key, tup.Item1, tup.Item2, Reader, DefaultCompressed)));
        }

        protected virtual BSAHeader ReadHeader()
        {
            return Reader.ReadStruct<BSAHeader>();
        }

        protected virtual List<KeyValuePair<string, FileRecord>> ReadFolders(uint folderCount)
        {
            var kvpList = new List<KeyValuePair<string, FileRecord>>();

            var folders = ReadFolderRecord(folderCount);
            foreach (var folder in folders)
            {
                string name;
                var frbs = ReadFileRecordBlocks(folder.count, out name);

                kvpList.AddRange(frbs.Select(fr => new KeyValuePair<string, FileRecord>(name, fr)));
            }

            return kvpList;
        }

        protected virtual List<FolderRecord> ReadFolderRecord(uint count)
        {
            var folders = new List<FolderRecord>((int)count);

            for (uint i = 0; i < count; i++)
                folders.Add(Reader.ReadStruct<FolderRecord>());

            return folders;
        }

        protected virtual List<FileRecord> ReadFileRecordBlocks(uint count, out string name)
        {
            var fileRecords = new List<FileRecord>((int)count);

            name = Reader.ReadBString(true);
            for (uint i = 0; i < count; i++)
            {
                var record = Reader.ReadStruct<FileRecord>();
                fileRecords.Add(record);
            }

            return fileRecords;
        }

        protected virtual List<string> ReadFileNameBlocks(uint count)
        {
            var fileNames = new List<string>((int)count);

            for (int i = 0; i < count; i++)
                fileNames.Add(Reader.ReadCString());

            return fileNames;
        }
    }
}