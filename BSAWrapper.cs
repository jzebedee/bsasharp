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
    public class BSAWrapper : ICollection<BSAFolder>
    {
        const int
            FALLOUT3_VERSION = 0x68,
            HEADER_OFFSET = 0x24; //Marshal.SizeOf(typeof(BSAHeader))

        static readonly char[] BSA_GREET = "BSA\0".ToCharArray();

        //private MemoryMappedFile BSAMap { get; set; }
        //private MemoryMappedBSAReader BSAReader { get; set; }

        private readonly SortedSet<BSAFolder> _folders;
        private readonly BSAHeader _readHeader;

        private readonly int _initSethash;

        private Dictionary<BSAFolder, long> _folderRecordOffsetsA = new Dictionary<BSAFolder, long>();
        private Dictionary<BSAFolder, uint> _folderRecordOffsetsB = new Dictionary<BSAFolder, uint>();

        private Dictionary<BSAFile, long> _fileRecordOffsetsA = new Dictionary<BSAFile, long>();
        private Dictionary<BSAFile, uint> _fileRecordOffsetsB = new Dictionary<BSAFile, uint>();

        private bool Modified { get { return _initSethash != _folders.GetHashCode(); } }

        public BSAWrapper(string bsaPath)
        {
            using (var BSAMap = MemoryMappedFile.CreateFromFile(bsaPath, FileMode.Open))
            using (var BSAReader = new MemoryMappedBSAReader(BSAMap))
            {
                _folders = new SortedSet<BSAFolder>(BSAReader.Read(), new BSAFolderComparer());
                _readHeader = BSAReader.Header;
            }

            _initSethash = _folders.GetHashCode();
        }
        public BSAWrapper()
        {
            _folders = new SortedSet<BSAFolder>(new BSAFolderComparer());
            _initSethash = _folders.GetHashCode();
        }

        public void Extract(string outFolder)
        {
            foreach (var folder in this)
            {
                Directory.CreateDirectory(Path.Combine(outFolder, folder.Path));

                foreach (var file in folder.Children)
                {
                    var filePath = Path.Combine(outFolder, file.Filename);
                    File.WriteAllBytes(filePath, file.GetSaveData(true));
                }
            }
        }

        public void Save(string outBsa)
        {
            var allFileNames = this.SelectMany(fold => fold.Children).Select(file => file.Name);

            File.Delete(outBsa);
            using (var writer = new BinaryWriter(File.OpenWrite(outBsa)))
            {
                var oldHeader = Modified ? null : _readHeader;
                var archFlags = _readHeader != null ? _readHeader.archiveFlags : ArchiveFlags.NamedDirectories | ArchiveFlags.NamedFiles;

                var header = oldHeader ?? new BSAHeader
                    {
                        field = BSA_GREET,
                        version = FALLOUT3_VERSION,
                        offset = HEADER_OFFSET,
                        archiveFlags = archFlags,
                        folderCount = (uint)this.Count(),
                        fileCount = (uint)allFileNames.Count(),
                        totalFolderNameLength = (uint)this.Sum(bsafolder => bsafolder.Path.Length + 1),
                        totalFileNameLength = (uint)allFileNames.Sum(file => file.Length + 1),
                        fileFlags = CreateFileFlags(allFileNames)
                    };

                writer.WriteStruct<BSAHeader>(header);

                var orderedFolders =
                    (from folder in this //presorted
                     let record = CreateFolderRecord(folder)
                     //orderby record.hash
                     select new { folder, record });

                orderedFolders.ToList()
                    .ForEach(a => WriteFolderRecord(writer, a.folder, a.record));

                //MUST execute this
                var fullyOrdered =
                    orderedFolders
                    .Select(a => CreateWriteFileRecordBlock(writer, a.folder)).ToList();

                allFileNames.ToList()
                    .ForEach(fileName => writer.WriteCString(fileName));

                (from folder in fullyOrdered
                 from file in folder
                 select file).ToList()
                 .ForEach(file =>
                 {
                     long offset = writer.BaseStream.Position;
                     _fileRecordOffsetsB.Add(file, (uint)offset);

                     writer.Write(file.GetSaveData(false));
                 });

                var folderRecordOffsets = _folderRecordOffsetsA.Zip(_folderRecordOffsetsB, (kvpA, kvpB) => new KeyValuePair<long, uint>(kvpA.Value, kvpB.Value));
                var fileRecordOffsets = _fileRecordOffsetsA.Zip(_fileRecordOffsetsB, (kvpA, kvpB) => new KeyValuePair<long, uint>(kvpA.Value, kvpB.Value));
                var completeOffsets = folderRecordOffsets.Concat(fileRecordOffsets);

                completeOffsets.ToList()
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
                    size = (uint)file.Size,
                    offset = 0
                };
        }

        private IEnumerable<BSAFile> CreateWriteFileRecordBlock(BinaryWriter writer, BSAFolder folder)
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

            //if this gets unwieldy, could foreach it and have a fall-through switch
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

        public void Add(BSAFolder item)
        {
            _folders.Add(item);
        }

        public void Clear()
        {
            _folders.Clear();
        }

        public bool Contains(BSAFolder item)
        {
            return _folders.Contains(item);
        }

        public void CopyTo(BSAFolder[] array, int arrayIndex)
        {
            _folders.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return _folders.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(BSAFolder item)
        {
            return _folders.Remove(item);
        }

        public IEnumerator<BSAFolder> GetEnumerator()
        {
            return _folders.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}