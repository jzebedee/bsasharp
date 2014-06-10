using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using BSAsharp.Format;
using BSAsharp.Extensions;
using System.Threading;

namespace BSAsharp
{
    public class BSAWrapper : IEnumerable<BSAFolder>, IDisposable
    {
        const int FALLOUT3_VERSION = 0x68;

        static readonly char[] BSA_GREET = "BSA\0".ToCharArray();

        private MemoryMappedFile BSAMap { get; set; }
        private MemoryMappedBSAReader BSAReader { get; set; }

        private Dictionary<BSAFolder, long> _folderRecordOffsetsA = new Dictionary<BSAFolder, long>();
        private Dictionary<BSAFolder, uint> _folderRecordOffsetsB = new Dictionary<BSAFolder, uint>();

        private Dictionary<BSAFile, long> _fileRecordOffsetsA = new Dictionary<BSAFile, long>();
        private Dictionary<BSAFile, uint> _fileRecordOffsetsB = new Dictionary<BSAFile, uint>();

        public BSAWrapper(string bsaPath)
        {
            this.BSAMap = MemoryMappedFile.CreateFromFile(bsaPath, FileMode.OpenOrCreate);//.CreateOrOpen(Path.GetFileName(Path.GetTempFileName()), MemoryMappedBSAReader.TWO_GB_SIZE, MemoryMappedFileAccess.ReadWrite);
            this.BSAReader = new MemoryMappedBSAReader(BSAMap);
        }
        ~BSAWrapper()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                BSAReader.Dispose();
                BSAMap.Dispose();
            }
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Save()
        {
            var allFileNames = this.SelectMany(fold => fold.Children).Select(file => file.Name);

            //using (var writer = new BinaryWriter(BSAMap.CreateViewStream(0, 0, MemoryMappedFileAccess.ReadWrite)))
            using (var writer = new BinaryWriter(File.OpenWrite(@"test.bsa")))
            {
                var header = new BSAHeader
                {
                    field = BSA_GREET,
                    version = FALLOUT3_VERSION,
                    offset = 0x24, //(uint)Marshal.SizeOf(typeof(BSAHeader)),
                    archiveFlags = ArchiveFlags.NamedDirectories | ArchiveFlags.NamedFiles,// | ArchiveFlags.Compressed,
                    folderCount = (uint)this.Count(),
                    fileCount = (uint)allFileNames.Count(),
                    totalFolderNameLength = (uint)this.Sum(bsafolder => bsafolder.Path.Length + 1),
                    totalFileNameLength = (uint)allFileNames.Sum(file => file.Length + 1),
                    fileFlags = CreateFileFlags(allFileNames)
                };

                writer.WriteStruct<BSAHeader>(header);

                var orderedFolders =
                    (from folder in this
                     let record = CreateFolderRecord(folder)
                     orderby record.hash
                     select new { folder, record }).ToList();

                orderedFolders
                    .ForEach(a => WriteFolderRecord(writer, a.folder, a.record));

                var fullyOrdered =
                    orderedFolders
                    .Select(a => WriteFileRecordBlock(writer, a.folder));

                allFileNames.ToList()
                    .ForEach(fileName => writer.WriteCString(fileName));

                (from folder in fullyOrdered
                 from file in folder
                 select file).ToList()
                 .ForEach(file =>
                 {
                     long offset = writer.BaseStream.Position;
                     _fileRecordOffsetsB.Add(file, (uint)offset);

                     writer.Write(file.Data);
                 });

                var folderRecordOffsets = _folderRecordOffsetsA.Zip(_folderRecordOffsetsB, (kvpA, kvpB) => new KeyValuePair<long, uint>(kvpA.Value, kvpB.Value));
                var fileRecordOffsets = _fileRecordOffsetsA.Zip(_fileRecordOffsetsB, (kvpA, kvpB) => new KeyValuePair<long, uint>(kvpA.Value, kvpB.Value));

                folderRecordOffsets.ToList()
                    .ForEach(kvp =>
                    {
                        writer.BaseStream.Seek(kvp.Key, SeekOrigin.Begin);
                        writer.Write(kvp.Value);
                    });
            }
        }

        private FolderRecord CreateFolderRecord(BSAFolder folder)
        {
            return
                new FolderRecord
                {
                    hash = Util.CreateHash(folder.Path, ""),
                    count = (uint)folder.Children.Count(),
                    offset = 0
                };
        }

        private void WriteFolderRecord(BinaryWriter writer, BSAFolder folder, FolderRecord rec)
        {
            long offset = writer.BaseStream.Position + Marshal.SizeOf(typeof(FolderRecord)) - sizeof(uint);
            _folderRecordOffsetsA.Add(folder, offset);

            writer.WriteStruct(rec);
        }

        private FileRecord CreateFileRecord(BSAFile file)
        {
            return
                new FileRecord
                {
                    hash = Util.CreateHash(Path.GetFileNameWithoutExtension(file.Name), Path.GetExtension(file.Name)),
                    size = (uint)file.Data.Length,
                    offset = 0
                };
        }

        private IEnumerable<BSAFile> WriteFileRecordBlock(BinaryWriter writer, BSAFolder folder)
        {
            long offset = writer.BaseStream.Position;
            _folderRecordOffsetsB.Add(folder, (uint)offset);

            writer.WriteBString(folder.Path);

            var sortedKids = (from file in folder.Children
                              let record = CreateFileRecord(file)
                              orderby record.hash
                              select new { file, record }).ToList();

            sortedKids.ForEach(a =>
            {
                offset = writer.BaseStream.Position + Marshal.SizeOf(typeof(FileRecord)) - sizeof(uint);
                _fileRecordOffsetsA.Add(a.file, offset);

                writer.WriteStruct(a.record);
            });

            return sortedKids.Select(a => a.file);
        }

        private FileFlags CreateFileFlags(IEnumerable<string> allFiles)
        {
            FileFlags flags = 0;

            //flatten children in folders, take extension of each bsafile name and convert to uppercase, take distinct
            var extSet = new HashSet<string>(allFiles.Select(filename => Path.GetExtension(filename).ToUpperInvariant()).Distinct());
            if (extSet.Contains(".NIF"))
                flags |= FileFlags.Nif;
            if (extSet.Contains(".DDS"))
                flags |= FileFlags.Dds;
            if (extSet.Contains(".XML"))
                flags |= FileFlags.Xml;
            if (extSet.Contains(".WAV"))
                flags |= FileFlags.Wav;
            if (extSet.Contains(".MP3") || extSet.Contains(".OGG"))
                flags |= FileFlags.Snd;
            if (extSet.Contains(".TXT") || extSet.Contains(".HTML") || extSet.Contains(".BAT") || extSet.Contains(".SCC"))
                flags |= FileFlags.Doc;
            if (extSet.Contains(".SPT"))
                flags |= FileFlags.Spt;
            if (extSet.Contains(".TEX") || extSet.Contains(".FNT"))
                flags |= FileFlags.Tex;
            if (extSet.Contains(".CTL"))
                flags |= FileFlags.Ctl;

            return flags;
        }

        public IEnumerator<BSAFolder> GetEnumerator()
        {
            return BSAReader.Read().GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}