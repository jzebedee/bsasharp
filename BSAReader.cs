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

        public void Read()
        {
            ReadHeader();
        }

        private void ReadHeader()
        {
            var field = _reader.ReadChars(4);
            var version = _reader.ReadUInt32();
            var offset = _reader.ReadUInt32();
            var archiveFlags = (ArchiveFlags)_reader.ReadUInt32();
            var folderCount = _reader.ReadUInt32();
            var fileCount = _reader.ReadUInt32();
            var totalFolderNameLength = _reader.ReadUInt32();
            var totalFileNameLength = _reader.ReadUInt32();
            var fileFlags = (FileFlags)_reader.ReadUInt32();

            var folderDict = new Dictionary<string, List<FileRecord>>();

            var folders = ReadFolderRecord(folderCount);
            foreach (var folder in folders)
            {
                string name;
                var frbs = ReadFileRecordBlocks(folder.count, out name);

                folderDict.Add(name, frbs);
            }

            BSAFile.DefaultCompressed = archiveFlags.HasFlag(ArchiveFlags.Compressed);

            var fileNames = ReadFileNameBlocks(fileCount);
            var fileNameDict = fileNames.ToDictionary(s => CreateHash(Path.GetFileNameWithoutExtension(s), Path.GetExtension(s)), s => s);

            var bsaFolders =
                from kvp in folderDict
                let path = kvp.Key
                let files = kvp.Value
                select new BSAFolder(path, files.Select(fr => new BSAFile(fileNameDict[fr.hash], fr, _reader)));

            //Trace.Assert(matches.SequenceEqual(folderDict.Values.SelectMany(s => s)));
            foreach (var folder in bsaFolders)
            {
                foreach (var child in folder.Children)
                {
                    Console.WriteLine(child.IsCompressed);
                }
            }
            Console.ReadKey();
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
                fileRecords.Add(_reader.ReadStruct<FileRecord>());

            return fileRecords;
        }

        private List<string> ReadFileNameBlocks(uint count)
        {
            var fileNames = new List<string>();
            for (int i = 0; i < count; i++)
                fileNames.Add(_reader.ReadCString());

            return fileNames;
        }

        private ulong CreateHash(string file, string ext)
        {
            Trace.Assert(file.Length > 0);
            ulong hash1 = (ulong)(file[file.Length - 1] | ((file.Length > 2 ? file[file.Length - 2] : 0) << 8) | file.Length << 16 | file[0] << 24);

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
            if (file.Length > 3)
                for (int i = 1; i < file.Length - 2; i++)
                    hash2 = (hash2 * 0x1003F) + file[i];

            for (int i = 0; i < ext.Length; i++)
                hash3 = (hash3 * 0x1003F) + ext[i];

            hash2 = ((hash2 << 32) + (hash3 << 32));

            return hash2 + hash1;
        }
    }
}