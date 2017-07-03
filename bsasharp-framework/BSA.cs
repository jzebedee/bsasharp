using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Threading.Tasks;
using BSAsharp.Extensions;
using BSAsharp.Format;
using BSAsharp.Progress;

namespace BSAsharp
{
    public class Bsa : SortedSet<BsaFolder>, IDisposable
    {
        internal const int
            FalloutVersion = 0x68,
            SizeRecordOffset = 0xC, //SIZE_RECORD - sizeof(uint)
            BsaGreet = 0x415342; //'B','S','A','\0'
        internal const uint BsaMaxSize = 0x80000000; //2 GiB

        private readonly BsaReader _bsaReader;

        private Dictionary<BsaFolder, uint> _folderRecordOffsetsA, _folderRecordOffsetsB;
        private Dictionary<BsaFile, uint> _fileRecordOffsetsA, _fileRecordOffsetsB;

        public ArchiveSettings Settings { get; private set; }

        /// <summary>
        /// Creates a new BSAWrapper instance around an existing BSA file
        /// </summary>
        /// <param name="bsaPath">The path of the file to open</param>
        /// <param name="options"></param>
        public Bsa(string bsaPath, CompressionOptions options = null)
            : this(MemoryMappedFile.CreateFromFile(bsaPath, FileMode.Open, null, 0L, MemoryMappedFileAccess.Read), options ?? new CompressionOptions())
        {
        }
        /// <summary>
        /// Creates a new BSAWrapper instance from an existing folder structure
        /// </summary>
        /// <param name="packFolder">The path of the folder to pack</param>
        /// <param name="settings">The archive settings used during packing</param>
        public Bsa(string packFolder, ArchiveSettings settings)
            : this(settings)
        {
            Pack(packFolder);
        }
        /// <summary>
        /// Creates an empty BSAWrapper instance that can be modified and saved to a BSA file
        /// </summary>
        public Bsa(ArchiveSettings settings)
            : base(BsaHashComparer.Instance)
        {
            Settings = settings;
        }

        //wtf C#
        //please get real ctor overloads someday
        private Bsa(MemoryMappedFile bsaMap, CompressionOptions options)
            : this(new BsaReader(bsaMap, options))
        {
        }
        private Bsa(BsaReader bsaReader)
            : base(bsaReader.Read(), BsaHashComparer.Instance)
        {
            Settings = bsaReader.Settings;

            _bsaReader = bsaReader;
        }
        ~Bsa()
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
                if (string.IsNullOrEmpty(g.Key))
                    throw new InvalidOperationException("BSAs may not contain top-level files");

                BsaFolder folder = this.SingleOrDefault(f => f.Path == g.Key);
                if (folder == null)
                    Add((folder = new BsaFolder(g.Key)));

                var realFiles = from f in g
                                let ext = Path.GetFileNameWithoutExtension(f)
                                where !string.IsNullOrEmpty(f)
                                select f;

                foreach (var file in realFiles)
                    folder.Add(new BsaFile(g.Key, Path.GetFileName(file), Settings, File.ReadAllBytes(file), false));
            }
        }

        public async Task UnpackAsync(string outFolder, IProgress<UnpackProgress> progress = null)
        {
            foreach (var folder in this)
                await folder.UnpackAsync(outFolder, progress);
        }

        public void Unpack(string outFolder)
        {
            foreach (var folder in this)
                folder.Unpack(outFolder);
        }

        public void Save(string outBsa, bool recreate = false)
        {
            outBsa = Path.GetFullPath(outBsa);

            var outBsaDir = Path.GetDirectoryName(outBsa);
            if (!string.IsNullOrEmpty(outBsaDir))
                Directory.CreateDirectory(outBsaDir);
            File.Delete(outBsa);

            using (var stream = File.OpenWrite(outBsa))
                SaveTo(stream, recreate);
        }

        private void SaveTo(Stream stream, bool recreate)
        {
            var allFiles = this.SelectMany(fold => fold).ToList();
            var allFileNames = allFiles.Select(file => file.Name).ToList();

            _folderRecordOffsetsA = new Dictionary<BsaFolder, uint>(Count);
            _folderRecordOffsetsB = new Dictionary<BsaFolder, uint>(Count);

            _fileRecordOffsetsA = new Dictionary<BsaFile, uint>(allFiles.Count);
            _fileRecordOffsetsB = new Dictionary<BsaFile, uint>(allFiles.Count);

            var header = new BsaHeader();
            if (!recreate && _bsaReader != null)
                header = _bsaReader.Header;
            if (header.Equals(default(BsaHeader)))
                //this needs to be set, otherwise we won't write archive information
                recreate = true;

            header.Field = BsaGreet;
            header.Version = FalloutVersion;
            header.Offset = BsaHeader.Size;
            if (recreate)
            {
                header.ArchiveFlags = ArchiveFlags.NamedDirectories | ArchiveFlags.NamedFiles;
                if (Settings.DefaultCompressed)
                    header.ArchiveFlags |= ArchiveFlags.Compressed;
                if (Settings.BStringPrefixed)
                    header.ArchiveFlags |= ArchiveFlags.BStringPrefixed;
            }
            header.FolderCount = (uint)Count;
            header.FileCount = (uint)allFileNames.Count;
            header.TotalFolderNameLength = (uint)this.Sum(folder => folder.Path.Length + 1);
            header.TotalFileNameLength = (uint)allFileNames.Sum(file => file.Length + 1);
            if (recreate)
            {
                header.FileFlags = CreateFileFlags(allFileNames);
            }

            using (var writer = new BinaryWriter(stream))
            {
                header.Write(writer);

                foreach (var folder in this)
                    WriteFolderRecord(writer, folder);

#if PARALLEL
                //parallel pump the files, as checking RecordSize may
                //trigger a decompress/recompress, depending on settings
                allFiles.AsParallel().ForAll(file => file.Cache());
#endif
                foreach (var folder in this)
                    WriteFileRecordBlock(writer, folder, header.TotalFileNameLength);

                allFileNames.ForEach(writer.WriteCString);

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

        private void WriteFileBlock(BinaryWriter writer, BsaFile file)
        {
            _fileRecordOffsetsB.Add(file, (uint)writer.BaseStream.Position);
            writer.Write(file.GetSaveData());
        }

        private void WriteFolderRecord(BinaryWriter writer, BsaFolder folder)
        {
            _folderRecordOffsetsA.Add(folder, (uint)writer.BaseStream.Position + SizeRecordOffset);
            folder.Record.Write(writer);
        }

        private void WriteFileRecordBlock(BinaryWriter writer, BsaFolder folder, uint totalFileNameLength)
        {
            _folderRecordOffsetsB.Add(folder, (uint)writer.BaseStream.Position + totalFileNameLength);
            writer.WriteBZString(folder.Path);

            foreach (var file in folder)
            {
                _fileRecordOffsetsA.Add(file, (uint)writer.BaseStream.Position + SizeRecordOffset);
                file.Record.Write(writer);
            }
        }

        private FileFlags CreateFileFlags(IEnumerable<string> allFiles)
        {
            FileFlags flags = 0;

            //take extension of each bsafile name, take distinct, convert to uppercase
            var extSet = new HashSet<string>(
                allFiles
                .Select(Path.GetExtension)
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