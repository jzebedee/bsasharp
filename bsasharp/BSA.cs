using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using BSAsharp.Extensions;
using BSAsharp.Format;

namespace BSAsharp
{
    public class BSA : SortedSet<BSAFolder>, IDisposable
    {
        internal const int
            FALLOUT_VERSION = 0x68,
            SIZE_RECORD_OFFSET = 0xC, //SIZE_RECORD - sizeof(uint)
            BSA_GREET = 0x415342; //'B','S','A','\0'
        internal const uint BSA_MAX_SIZE = 0x80000000; //2 GiB

        private readonly BSAReader _bsaReader;

        private Dictionary<BSAFolder, uint> _folderRecordOffsetsA, _folderRecordOffsetsB;
        private Dictionary<BSAFile, uint> _fileRecordOffsetsA, _fileRecordOffsetsB;

        public ArchiveSettings Settings { get; private set; }

        /// <summary>
        /// Creates a new BSAWrapper instance around an existing BSA file
        /// </summary>
        /// <param name="bsaPath">The path of the file to open</param>
        public BSA(string bsaPath, CompressionOptions Options = null)
            : this(MemoryMappedFile.CreateFromFile(bsaPath, FileMode.Open, null, 0L, MemoryMappedFileAccess.Read), Options ?? new CompressionOptions())
        {
        }
        /// <summary>
        /// Creates a new BSAWrapper instance from an existing folder structure
        /// </summary>
        /// <param name="packFolder">The path of the folder to pack</param>
        /// <param name="settings">The archive settings used during packing</param>
        public BSA(string packFolder, ArchiveSettings settings)
            : this(settings)
        {
            Pack(packFolder);
        }
        /// <summary>
        /// Creates an empty BSAWrapper instance that can be modified and saved to a BSA file
        /// </summary>
        public BSA(ArchiveSettings settings)
            : base(BSAHashComparer.Instance)
        {
            this.Settings = settings;
        }

        //wtf C#
        //please get real ctor overloads someday
        private BSA(MemoryMappedFile BSAMap, CompressionOptions Options)
            : this(new BSAReader(BSAMap, Options))
        {
        }
        private BSA(BSAReader BSAReader)
            : base(BSAReader.Read(), BSAHashComparer.Instance)
        {
            this.Settings = BSAReader.Settings;

            this._bsaReader = BSAReader;
        }
        ~BSA()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_bsaReader != null)
                    _bsaReader.Dispose();
            }
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Pack(string packFolder)
        {
            if (!Directory.Exists(packFolder))
                throw new DirectoryNotFoundException("Folder \"" + packFolder + "\" does not exist");

            var groupedFiles =
                from file in Directory.EnumerateFiles(packFolder, "*", SearchOption.AllDirectories)
                let folderName = Path.GetDirectoryName(file).TrimStart(packFolder)
                group file by folderName into g
                select g;

            foreach (var g in groupedFiles)
            {
                BSAFolder folder = this.SingleOrDefault(f => f.Path == g.Key);
                if (folder == null)
                    Add((folder = new BSAFolder(g.Key)));

                var realFiles = from f in g
                                let ext = Path.GetFileNameWithoutExtension(f)
                                where !string.IsNullOrEmpty(f)
                                select f;

                foreach (var file in realFiles)
                    folder.Add(new BSAFile(g.Key, Path.GetFileName(file), Settings, File.ReadAllBytes(file), false));
            }
        }

        public void Unpack(string outFolder)
        {
            foreach (var folder in this)
                folder.Unpack(outFolder);
        }

        public void Save(string outBsa, bool recreate = false)
        {
            outBsa = Path.GetFullPath(outBsa);
            Directory.CreateDirectory(Path.GetDirectoryName(outBsa));
            File.Delete(outBsa);

            using (var stream = File.OpenWrite(outBsa))
                SaveTo(stream, recreate);
        }

        private void SaveTo(Stream stream, bool recreate)
        {
            var allFiles = this.SelectMany(fold => fold).ToList();
            var allFileNames = allFiles.Select(file => file.Name).ToList();

            _folderRecordOffsetsA = new Dictionary<BSAFolder, uint>(Count);
            _folderRecordOffsetsB = new Dictionary<BSAFolder, uint>(Count);

            _fileRecordOffsetsA = new Dictionary<BSAFile, uint>(allFiles.Count);
            _fileRecordOffsetsB = new Dictionary<BSAFile, uint>(allFiles.Count);

            BSAHeader header = new BSAHeader();
            if (!recreate && _bsaReader != null)
                header = _bsaReader.Header;
            if (header.Equals(default(BSAHeader)))
                //this needs to be set, otherwise we won't write archive information
                recreate = true;

            header.field = BSA_GREET;
            header.version = FALLOUT_VERSION;
            header.offset = BSAHeader.Size;
            if (recreate)
            {
                header.archiveFlags = ArchiveFlags.NamedDirectories | ArchiveFlags.NamedFiles;
                if (Settings.DefaultCompressed)
                    header.archiveFlags |= ArchiveFlags.Compressed;
                if (Settings.BStringPrefixed)
                    header.archiveFlags |= ArchiveFlags.BStringPrefixed;
            }
            header.folderCount = (uint)Count;
            header.fileCount = (uint)allFileNames.Count;
            header.totalFolderNameLength = (uint)this.Sum(folder => folder.Path.Length + 1);
            header.totalFileNameLength = (uint)allFileNames.Sum(file => file.Length + 1);
            if (recreate)
            {
                header.fileFlags = CreateFileFlags(allFileNames);
            }

            using (var writer = new BinaryWriter(stream))
            {
                header.Write(writer);

                foreach (var folder in this)
                    WriteFolderRecord(writer, folder, CreateFolderRecord(folder));

#if PARALLEL
                //parallel pump the files, as checking RecordSize may
                //trigger a decompress/recompress, depending on settings
                allFiles.AsParallel().ForAll(file => file.Cache());
#endif
                foreach (var folder in this)
                    WriteFileRecordBlock(writer, folder, header.totalFileNameLength);

                allFileNames.ForEach(fileName => writer.WriteCString(fileName));

                allFiles.ForEach(file => WriteFileBlock(writer, file));

                var folderRecordOffsets = _folderRecordOffsetsA.Zip(_folderRecordOffsetsB, (kvpA, kvpB) => new KeyValuePair<uint, uint>(kvpA.Value, kvpB.Value));
                var fileRecordOffsets = _fileRecordOffsetsA.Zip(_fileRecordOffsetsB, (kvpA, kvpB) => new KeyValuePair<uint, uint>(kvpA.Value, kvpB.Value));
                var completeOffsets = folderRecordOffsets.Concat(fileRecordOffsets).ToList();

                completeOffsets.ForEach(kvp =>
                {
                    writer.BaseStream.Seek(kvp.Key, SeekOrigin.Begin);
                    writer.Write(kvp.Value);
                });
            }
        }

        private void WriteFileBlock(BinaryWriter writer, BSAFile file)
        {
            _fileRecordOffsetsB.Add(file, (uint)writer.BaseStream.Position);
            writer.Write(file.GetSaveData());
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

        private void WriteFolderRecord(BinaryWriter writer, BSAFolder folder, FolderRecord rec)
        {
            _folderRecordOffsetsA.Add(folder, (uint)writer.BaseStream.Position + SIZE_RECORD_OFFSET);
            rec.Write(writer);
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

        private void WriteFileRecordBlock(BinaryWriter writer, BSAFolder folder, uint totalFileNameLength)
        {
            _folderRecordOffsetsB.Add(folder, (uint)writer.BaseStream.Position + totalFileNameLength);
            writer.WriteBZString(folder.Path);

            foreach (var file in folder)
            {
                var record = CreateFileRecord(file);

                _fileRecordOffsetsA.Add(file, (uint)writer.BaseStream.Position + SIZE_RECORD_OFFSET);
                record.Write(writer);
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