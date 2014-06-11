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
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace BSAsharp
{
    public class BSAWrapper : SortedSet<BSAFolder>
    {
        public const int
            FALLOUT3_VERSION = 0x68,
            HEADER_OFFSET = 0x24; //Marshal.SizeOf(typeof(BSAHeader))

        static readonly char[] BSA_GREET = "BSA\0".ToCharArray();

        private readonly BSAHeader _readHeader;

        private readonly bool _defaultCompress;

        private Dictionary<BSAFolder, long> _folderRecordOffsetsA = new Dictionary<BSAFolder, long>();
        private Dictionary<BSAFolder, uint> _folderRecordOffsetsB = new Dictionary<BSAFolder, uint>();

        private Dictionary<BSAFile, long> _fileRecordOffsetsA = new Dictionary<BSAFile, long>();
        private Dictionary<BSAFile, uint> _fileRecordOffsetsB = new Dictionary<BSAFile, uint>();

        /// <summary>
        /// Creates a new BSAWrapper instance around an existing BSA file
        /// </summary>
        /// <param name="bsaPath">The path of the file to open</param>
        public BSAWrapper(string bsaPath)
            : this(MemoryMappedFile.CreateFromFile(bsaPath, FileMode.Open))
        {
        }
        /// <summary>
        /// Creates a new BSAWrapper instance from an existing folder structure
        /// </summary>
        /// <param name="packFolder">The path of the folder to pack</param>
        /// <param name="defaultCompressed">The default compression state for the archive</param>
        public BSAWrapper(string packFolder, bool defaultCompressed)
            : this()
        {
            this._defaultCompress = defaultCompressed;
            Pack(packFolder);
        }
        /// <summary>
        /// Creates an empty BSAWrapper instance that can be modified and saved to a BSA file
        /// </summary>
        public BSAWrapper()
            : this(new SortedSet<BSAFolder>())
        {
        }

        //wtf C#
        //please get real ctor overloads someday
        private BSAWrapper(MemoryMappedFile BSAMap)
            : this(new MemoryMappedBSAReader(BSAMap))
        {
            BSAMap.Dispose();
        }
        private BSAWrapper(MemoryMappedBSAReader BSAReader)
            : this(BSAReader.Read())
        {
            this._readHeader = BSAReader.Header;
            BSAReader.Dispose();
        }
        private BSAWrapper(IEnumerable<BSAFolder> collection)
            : base(collection, HashComparer.Instance)
        {
        }

        public void Pack(string packFolder)
        {
            var packDirectories = Directory.EnumerateDirectories(packFolder, "*", SearchOption.AllDirectories);
            var bsaFolders = packDirectories
                .Select(path =>
                {
                    var packFiles = Directory.EnumerateFiles(path);

                    var trimmedPath = path.Replace(packFolder, "").TrimStart(Path.DirectorySeparatorChar);
                    var bsaFiles = packFiles.Select(file => new BSAFile(trimmedPath, Path.GetFileName(file), File.ReadAllBytes(file), _defaultCompress, false));

                    return new BSAFolder(trimmedPath, bsaFiles);
                });

            bsaFolders.ToList()
                .ForEach(folder => Add(folder));
        }

        public void Extract(string outFolder)
        {
            if (Directory.Exists(outFolder))
                Directory.Delete(outFolder, true);

            foreach (var folder in this)
            {
                Directory.CreateDirectory(Path.Combine(outFolder, folder.Path));

                foreach (var file in folder)
                {
                    var filePath = Path.Combine(outFolder, file.Filename);
                    File.WriteAllBytes(filePath, file.GetSaveData(true));
                }
            }
        }

        public void Save(string outBsa, bool recheck = true)
        {
            var allFileNames = this.SelectMany(fold => fold).Select(file => file.Name);

            File.Delete(outBsa);
            using (var writer = new BinaryWriter(File.OpenWrite(outBsa)))
            {
                var archFlags = _readHeader != null ? _readHeader.archiveFlags : ArchiveFlags.NamedDirectories | ArchiveFlags.NamedFiles | (_defaultCompress ? ArchiveFlags.Compressed : 0);

                var header =
                    (recheck ? null : _readHeader) ?? new BSAHeader
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
                    hash = folder.Hash,
                    count = (uint)folder.Count(),
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
                    hash = file.Hash,
                    size = file.Size,
                    offset = 0
                };
        }

        private IEnumerable<BSAFile> CreateWriteFileRecordBlock(BinaryWriter writer, BSAFolder folder)
        {
            long offset = writer.BaseStream.Position;
            _folderRecordOffsetsB.Add(folder, (uint)offset);

            writer.WriteBString(folder.Path);

            var sortedKids = (from file in folder
                              let record = CreateFileRecord(file)
                              //orderby record.hash
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
    }
}