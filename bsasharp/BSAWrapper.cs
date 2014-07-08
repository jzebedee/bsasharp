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
using System.Diagnostics;

namespace BSAsharp
{
    public class BSAWrapper : SortedSet<BSAFolder>, IDisposable
    {
        public const int
            FALLOUT_VERSION = 0x68,
            HEADER_OFFSET = 0x24, //sizeof(BSAHeader)
            SIZE_RECORD = 0x10, //sizeof(FolderRecord) or sizeof(FileRecord)
            SIZE_RECORD_OFFSET = 0xC, //SIZE_RECORD - sizeof(uint)
            BSA_GREET = 0x415342; //'B','S','A','\0'
        public const uint BSA_MAX_SIZE = 0x80000000; //2 GiB

        private readonly BSAHeader _readHeader;

        private Dictionary<BSAFolder, uint> _folderRecordOffsetsA;
        private Dictionary<BSAFolder, uint> _folderRecordOffsetsB;

        private Dictionary<BSAFile, uint> _fileRecordOffsetsA;
        private Dictionary<BSAFile, uint> _fileRecordOffsetsB;

        public ArchiveSettings Settings { get; private set; }

        /// <summary>
        /// Creates a new BSAWrapper instance around an existing BSA file
        /// </summary>
        /// <param name="bsaPath">The path of the file to open</param>
        public BSAWrapper(string bsaPath, CompressionOptions Options = null)
            : this(MemoryMappedFile.CreateFromFile(bsaPath), Options ?? new CompressionOptions())
        {
        }
        /// <summary>
        /// Creates a new BSAWrapper instance from an existing folder structure
        /// </summary>
        /// <param name="packFolder">The path of the folder to pack</param>
        /// <param name="defaultCompressed">The default compression state for the archive</param>
        public BSAWrapper(string packFolder, ArchiveSettings settings)
            : this(settings)
        {
            Pack(packFolder);
        }
        /// <summary>
        /// Creates an empty BSAWrapper instance that can be modified and saved to a BSA file
        /// </summary>
        public BSAWrapper(ArchiveSettings settings)
            : this(new SortedSet<BSAFolder>())
        {
            this.Settings = settings;
        }

        //wtf C#
        //please get real ctor overloads someday
        private BSAWrapper(MemoryMappedFile BSAMap, CompressionOptions Options)
            : this(new BSAReader(BSAMap, Options))
        {
            this._bsaMap = BSAMap;
        }
        private BSAWrapper(BSAReader BSAReader)
            : this(BSAReader.Read())
        {
            this._readHeader = BSAReader.Header;
            this.Settings = BSAReader.Settings;

            this._bsaReader = BSAReader;
        }
        private BSAWrapper(IEnumerable<BSAFolder> collection)
            : base(collection, HashComparer.Instance)
        {
        }
        ~BSAWrapper()
        {
            Dispose(false);
        }

        private readonly BSAReader _bsaReader;
        private readonly MemoryMappedFile _bsaMap;

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_bsaReader != null)
                    _bsaReader.Dispose();
                if (_bsaMap != null)
                    _bsaMap.Dispose();
            }
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Pack(string packFolder)
        {
            var packDirectories = Directory.EnumerateDirectories(packFolder, "*", SearchOption.AllDirectories);
            var bsaFolders = packDirectories
                .Select(path =>
                {
                    var packFiles = Directory.EnumerateFiles(path);

                    var trimmedPath = path.Replace(packFolder, "").TrimStart(Path.DirectorySeparatorChar);
                    var bsaFiles = from file in packFiles
                                   let fileName = Path.GetFileName(file)
                                   let fnNoExt = Path.GetFileNameWithoutExtension(fileName)
                                   where !string.IsNullOrEmpty(fnNoExt)
                                   select new BSAFile(trimmedPath, fileName, Settings, File.ReadAllBytes(file), false);

                    return new BSAFolder(trimmedPath, bsaFiles);
                });

            bsaFolders.ToList()
                .ForEach(folder => Add(folder));
        }

        public void Extract(string outFolder)
        {
            foreach (var folder in this)
            {
                Directory.CreateDirectory(Path.Combine(outFolder, folder.Path));

                foreach (var file in folder)
                {
                    var filePath = Path.Combine(outFolder, file.Filename);
                    File.WriteAllBytes(filePath, file.GetContents(true));
                }
            }
        }

        public void Save(string outBsa, bool recreate = false)
        {
            BSAHeader header;
            if (!recreate)
                header = _readHeader;
            else
                header = new BSAHeader();

            _folderRecordOffsetsA = new Dictionary<BSAFolder, uint>();
            _folderRecordOffsetsB = new Dictionary<BSAFolder, uint>();

            _fileRecordOffsetsA = new Dictionary<BSAFile, uint>();
            _fileRecordOffsetsB = new Dictionary<BSAFile, uint>();

            var allFiles = this.SelectMany(fold => fold).ToList();
            var allFileNames = allFiles.Select(file => file.Name).ToList();

            outBsa = Path.GetFullPath(outBsa);
            Directory.CreateDirectory(Path.GetDirectoryName(outBsa));

            File.Delete(outBsa);
            using (var mmf = MemoryMappedFile.CreateFromFile(outBsa, FileMode.Create, Path.GetRandomFileName(), BSA_MAX_SIZE, MemoryMappedFileAccess.ReadWrite))
            {
                header.field = BSA_GREET;
                header.version = FALLOUT_VERSION;
                header.offset = HEADER_OFFSET;
                if (recreate)
                {
                    header.archiveFlags = ArchiveFlags.NamedDirectories | ArchiveFlags.NamedFiles;
                    if (Settings.DefaultCompressed)
                        header.archiveFlags |= ArchiveFlags.Compressed;
                    if (Settings.BStringPrefixed)
                        header.archiveFlags |= ArchiveFlags.BStringPrefixed;
                }
                header.folderCount = (uint)this.Count();
                header.fileCount = (uint)allFileNames.Count;
                header.totalFolderNameLength = (uint)this.Sum(bsafolder => bsafolder.Path.Length + 1);
                header.totalFileNameLength = (uint)allFileNames.Sum(file => file.Length + 1);
                if (recreate)
                {
                    header.fileFlags = CreateFileFlags(allFileNames);
                }

                uint offset = 0;
                using (var writer = mmf.ToWriter<BSAHeader>(offset))
                    header.Write(writer);
                offset += HEADER_OFFSET;

                var orderedFolders = this.Select(folder => new { folder, record = CreateFolderRecord(folder) }).ToList();

                orderedFolders.ForEach(a => WriteFolderRecord(mmf, ref offset, a.folder, a.record));

                orderedFolders.ForEach(a => WriteFileRecordBlock(mmf, ref offset, a.folder, header.totalFileNameLength));

                using (var writer = mmf.ToWriter(offset, header.totalFileNameLength))
                    allFileNames.ForEach(fileName => writer.WriteCString(fileName));
                offset += header.totalFileNameLength;

                allFiles.ForEach(file => WriteFileBlock(mmf, ref offset, file));

                var folderRecordOffsets = _folderRecordOffsetsA.Zip(_folderRecordOffsetsB, (kvpA, kvpB) => new KeyValuePair<uint, uint>(kvpA.Value, kvpB.Value));
                var fileRecordOffsets = _fileRecordOffsetsA.Zip(_fileRecordOffsetsB, (kvpA, kvpB) => new KeyValuePair<uint, uint>(kvpA.Value, kvpB.Value));
                var completeOffsets = folderRecordOffsets.Concat(fileRecordOffsets);

                completeOffsets
                    .ToList()
                    .ForEach(kvp =>
                    {
                        using (var writer = mmf.ToWriter(kvp.Key, sizeof(uint)))
                            writer.Write(kvp.Value);
                    });
            }
        }

        private void WriteFileBlock(MemoryMappedFile mmf, ref uint offset, BSAFile file)
        {
            _fileRecordOffsetsB.Add(file, offset);

            var saveBytes = file.GetSaveData();
            using (var writer = mmf.ToWriter(offset, saveBytes.LongLength))
                writer.Write(saveBytes);

            offset += (uint)saveBytes.LongLength;
        }

        private FolderRecord CreateFolderRecord(BSAFolder folder)
        {
            return new FolderRecord
            {
                hash = folder.Hash,
                count = (uint)folder.Count(),
                offset = 0
            };
        }

        private void WriteFolderRecord(MemoryMappedFile mmf, ref uint offset, BSAFolder folder, FolderRecord rec)
        {
            using (var writer = mmf.ToWriter(offset, SIZE_RECORD))
            {
                _folderRecordOffsetsA.Add(folder, offset + SIZE_RECORD_OFFSET);
                rec.Write(writer);

                offset += SIZE_RECORD;
            }
        }

        private FileRecord CreateFileRecord(BSAFile file)
        {
            return new FileRecord
            {
                hash = file.Hash,
                size = file.CalculateRecordSize(),
                offset = 0
            };
        }

        private void WriteFileRecordBlock(MemoryMappedFile mmf, ref uint offset, BSAFolder folder, uint totalFileNameLength)
        {
            var size = (folder.Count * SIZE_RECORD) + (BinaryExtensions.Windows1252.GetByteCount(folder.Path) + 2); //length byte + null byte
            using (var writer = mmf.ToWriter(offset, size))
            {
                _folderRecordOffsetsB.Add(folder, offset + totalFileNameLength);
                writer.WriteBZString(folder.Path);

                foreach (var file in folder)
                {
                    var record = CreateFileRecord(file);

                    _fileRecordOffsetsA.Add(file, offset + (uint)writer.BaseStream.Position + SIZE_RECORD_OFFSET);
                    record.Write(writer);
                }

                offset += (uint)size;
            }
        }

        private FileFlags CreateFileFlags(IEnumerable<string> allFiles)
        {
            FileFlags flags = 0;

            //take extension of each bsafile name, take distinct, convert to uppercase
            var extSet = new HashSet<string>(
                allFiles
                .Select(filename => Path.GetExtension(filename))
                .Distinct()
                .Select(ext => ext.ToUpperInvariant()));

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
                flags |= FileFlags.Mp3;
            if (extSet.Contains(".TXT") || extSet.Contains(".HTML") || extSet.Contains(".BAT") || extSet.Contains(".SCC"))
                flags |= FileFlags.Txt;
            if (extSet.Contains(".SPT"))
                flags |= FileFlags.Spt;
            if (extSet.Contains(".TEX") || extSet.Contains(".FNT"))
                flags |= FileFlags.Tex;
            if (extSet.Contains(".CTL") || extSet.Contains(".DLODSETTINGS")) //https://github.com/Ethatron/bsaopt/issues/13
                flags |= FileFlags.Ctl;

            return flags;
        }
    }
}