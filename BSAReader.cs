using BSAsharp.Format;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BSAsharp
{
    public class BSAReader : IDisposable
    {
        private readonly BinaryReader _reader;

        public BSAReader(Stream bsaStream)
        {
            this._reader = new BinaryReader(bsaStream);
        }
        ~BSAReader()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _reader.Dispose();
            }
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public IEnumerable<BSAFolder> Read()
        {
            var header = ReadHeader();
            var folderDict = ReadFolders(header.folderCount);

            BSAFile.DefaultCompressed = header.archiveFlags.HasFlag(ArchiveFlags.Compressed);
            BSAFile.BStringPrefixed = header.archiveFlags.HasFlag(ArchiveFlags.BStringPrefixed);

            var fileNames = ReadFilenames(header.fileCount);

            return BuildBSALayout(folderDict, fileNames);
        }

        private BSAHeader ReadHeader()
        {
            return _reader.ReadStruct<BSAHeader>();
        }

        private List<BSAFolder> BuildBSALayout(Dictionary<string, List<FileRecord>> folderDict, List<string> fileNames)
        {
            int i = 0;
            return (from kvp in folderDict
                   let path = kvp.Key
                   let fileRecs = kvp.Value
                   select new BSAFolder(path, fileRecs.Select(fr => new BSAFile(path, fileNames[i++], fr, _reader)))).ToList();
        }

        private List<string> ReadFilenames(uint fileCount)
        {
            return (from fileName in ReadFileNameBlocks(fileCount)
                    //let hash = CreateHash(Path.GetFileNameWithoutExtension(fileName), Path.GetExtension(fileName))
                    select fileName).ToList();
        }

        private Dictionary<string, List<FileRecord>> ReadFolders(uint folderCount)
        {
            var folderDict = new Dictionary<string, List<FileRecord>>();

            var folders = ReadFolderRecord(folderCount);
            foreach (var folder in folders)
            {
                string name;
                var frbs = ReadFileRecordBlocks(folder.count, out name);

                folderDict.Add(name, frbs);
            }

            return folderDict;
        }

        private List<FolderRecord> ReadFolderRecord(uint count)
        {
            var folders = new List<FolderRecord>((int)count);

            for (uint i = 0; i < count; i++)
                folders.Add(_reader.ReadStruct<FolderRecord>());

            return folders;
        }

        private List<FileRecord> ReadFileRecordBlocks(uint count, out string name)
        {
            var fileRecords = new List<FileRecord>();

            name = _reader.ReadBString(true);
            for (uint i = 0; i < count; i++)
            {
                var record = _reader.ReadStruct<FileRecord>();
                fileRecords.Add(record);
            }

            return fileRecords;
        }

        private List<string> ReadFileNameBlocks(uint count)
        {
            var fileNames = new List<string>();

            for (int i = 0; i < count; i++)
                fileNames.Add(_reader.ReadCString());

            return fileNames;
        }

        private static ulong CreateHash(string fname, string ext)
        {
            Trace.Assert(fname.Length > 0);
            ulong hash1 = (ulong)(fname[fname.Length - 1] | ((fname.Length > 2 ? fname[fname.Length - 2] : 0) << 8) | fname.Length << 16 | fname[0] << 24);

            Trace.Assert(ext.Length > 0);
            switch (ext)
            {
                case ".kf":
                    hash1 |= 0x80;
                    break;
                case ".nif":
                    hash1 |= 0x8000;
                    break;
                case ".dds":
                    hash1 |= 0x8080;
                    break;
                case ".wav":
                    hash1 |= 0x80000000;
                    break;
            }

            ulong hash2 = 0, hash3 = 0;
            if (fname.Length > 3)
                for (int i = 1; i < fname.Length - 2; i++)
                    hash2 = (hash2 * 0x1003F) + fname[i];

            for (int i = 0; i < ext.Length; i++)
                hash3 = (hash3 * 0x1003F) + ext[i];

            hash2 = ((hash2 << 32) + (hash3 << 32));

            return hash2 + hash1;
        }
    }
}