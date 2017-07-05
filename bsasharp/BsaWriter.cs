using BSAsharp;
using BSAsharp.Extensions;
using BSAsharp.Format;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace bsasharp
{
    public class BsaWriter
    {
        private readonly Bsa _bsa;

        public BsaWriter(Bsa bsa)
        {
            _bsa = bsa;
        }

        public void Save(string path, BsaHeader header)
        {
#if UNSAFE
            using (var mmf = MemoryMappedFile.CreateFromFile(path, FileMode.Create, null, Bsa.BsaMaxSize, MemoryMappedFileAccess.ReadWrite))
            {
                SaveUnsafe(mmf, header);
            }
#else
            using (var stream = File.Create(path))
            {
                Save(stream, header);
            }
#endif
        }

#if UNSAFE
        public void SaveUnsafe(MemoryMappedFile mmf, BsaHeader header)
        {
            var allFiles = _bsa.SelectMany(fold => fold).ToList();
            var allFileNames = allFiles.Select(file => file.Name).ToList();

            header.Field = Bsa.BsaGreet;
            header.Version = Bsa.FalloutVersion;
            header.Offset = BsaHeader.Size;
            header.FolderCount = (uint)_bsa.Count;
            header.FileCount = (uint)allFileNames.Count;
            header.TotalFolderNameLength = (uint)_bsa.Sum(folder => folder.Path.Length + 1);
            header.TotalFileNameLength = (uint)allFileNames.Sum(file => file.Length + 1);

            long offset = 0;
            using (var acc = mmf.CreateViewAccessor())
            {
                acc.Write(offset, ref header);
                offset += header.Offset;

                var folderRecords = _bsa.Select(folder => folder.Record).ToArray();
                acc.WriteArray(offset, folderRecords, 0, folderRecords.Length);
                offset += Marshal.SizeOf(typeof(FolderRecord)) * folderRecords.Length;
            }

            var namedDirectories = header.ArchiveFlags.HasFlag(ArchiveFlags.NamedDirectories);
            var defaultCompress = header.ArchiveFlags.HasFlag(ArchiveFlags.DefaultCompressed);

            var folderRecordOffsets = _bsa
                .Select((folder, i) => new { folder, folderOffset = header.Offset + (Marshal.SizeOf(typeof(FolderRecord)) * i) })
                .ToDictionary(a => a.folder, a => a.folderOffset);
            var folderFileBlockOffsets = new Dictionary<BsaFolder, uint>();

            var fileRecordOffsets = new Dictionary<BethesdaFile, uint>();
            var fileDataSizes = new Dictionary<BethesdaFile, uint>();
            var fileDataOffsets = new Dictionary<BethesdaFile, uint>();

            using (var stream = mmf.CreateViewStream())
            using (var writer = new BinaryWriter(stream))
            {
                writer.BaseStream.Seek(offset, SeekOrigin.Begin);
                foreach (var folder in _bsa)
                {
                    var folderFilesOffset = offset + header.TotalFileNameLength;
                    folderFileBlockOffsets.Add(folder, (uint)folderFilesOffset);

                    if (namedDirectories)
                    {
                        writer.WriteBZString(folder.Path);
                    }

                    foreach (var file in folder)
                    {
                        fileRecordOffsets.Add(file, (uint)writer.BaseStream.Position);
                        var fileRecord = new FileRecord
                        {
                            hash = file.Hash
                        };
                        fileRecord.Write(writer);
                    }

                    offset = writer.BaseStream.Position;
                }

                allFileNames.ForEach(writer.WriteCString);
                offset = writer.BaseStream.Position;
            }

            var folders = folderRecordOffsets
                .Zip(folderFileBlockOffsets, (a, b) => new { folderRecordOffset = (uint)a.Value + Bsa.SizeRecordOffset, offsetValue = b.Value });
            using (var acc = mmf.CreateViewAccessor())
            {
                foreach (var folder in folders)
                {
                    acc.Write(folder.folderRecordOffset, folder.offsetValue);
                }
            }

            var bstringPrefixed = header.ArchiveFlags.HasFlag(ArchiveFlags.BStringPrefixed);
            using (var stream = mmf.CreateViewStream())
            using (var writer = new BinaryWriter(stream))
            {
                writer.BaseStream.Seek(offset, SeekOrigin.Begin);
                foreach (var file in allFiles)
                {
                    var fileDataOffset = (uint)writer.BaseStream.Position;
                    fileDataOffsets.Add(file, fileDataOffset);

                    uint size;

                    if (bstringPrefixed)
                    {
                        writer.WriteBString(file.Filename);
                    }
                    if (file.IsCompressFlagSet ^ defaultCompress)
                    {
                        var beginOffset = (uint)writer.BaseStream.Position;
                        //write compressed
                        writer.Write((uint)file.Data.Length);
                        var zlib = new Zlib();
                        using (var dataStream = new MemoryStream(file.Data))
                        using (var deflateStream = zlib.CompressStream(writer.BaseStream, System.IO.Compression.CompressionLevel.Optimal))
                        {
                            dataStream.CopyTo(deflateStream);
                        }
                        size = (uint)(writer.BaseStream.Position - beginOffset);
                    }
                    else
                    {
                        //write normal
                        writer.Write(file.Data);
                        size = (uint)file.Data.Length;
                    }

                    if (file.IsCompressFlagSet)
                    {
                        size |= BethesdaFile.FlagCompress;
                    }

                    fileDataSizes.Add(file, size);
                }
            }

            var files = fileRecordOffsets
                .Zip(fileDataOffsets, (a, b) => new { file = a.Key, fileRecordOffset = a.Value, offsetValue = b.Value })
                .Zip(fileDataSizes, (a, b) => new { a.file, a.fileRecordOffset, a.offsetValue, size = b.Value });
            using (var acc = mmf.CreateViewAccessor())
            {
                unsafe
                {
                    try
                    {
                        byte* ptr = (byte*)0;
                        acc.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);

                        foreach (var file in files)
                        {
                            var fileRecord = (FileRecord*)(ptr + file.fileRecordOffset);
                            fileRecord->size = file.size;
                            fileRecord->offset = file.offsetValue;
                        }
                    }
                    finally
                    {
                        acc.SafeMemoryMappedViewHandle.ReleasePointer();
                    }
                }
            }
        }
#else
        private void Save(Stream stream, BsaHeader header)
        {
            var allFiles = _bsa.SelectMany(fold => fold).ToList();
            var allFileNames = allFiles.Select(file => file.Name).ToList();

            header.Field = Bsa.BsaGreet;
            header.Version = Bsa.FalloutVersion;
            header.Offset = (uint)Marshal.SizeOf(typeof(BsaHeader));
            header.FolderCount = (uint)_bsa.Count;
            header.FileCount = (uint)allFileNames.Count;
            header.TotalFolderNameLength = (uint)_bsa.Sum(folder => folder.Path.Length + 1);
            header.TotalFileNameLength = (uint)allFileNames.Sum(file => file.Length + 1);

            var headerBytes = BinaryExtensions.GetBytes(header);
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(headerBytes);

                foreach (var record in _bsa.Select(folder => folder.Record))
                {
                    var folderRecordBytes = BinaryExtensions.GetBytes(record);
                    writer.Write(folderRecordBytes);
                }

                var namedDirectories = header.ArchiveFlags.HasFlag(ArchiveFlags.NamedDirectories);
                var defaultCompress = header.ArchiveFlags.HasFlag(ArchiveFlags.DefaultCompressed);

                var folderRecordOffsets = _bsa
                    .Select((folder, i) => new { folder, folderOffset = header.Offset + (Marshal.SizeOf(typeof(FolderRecord)) * i) })
                    .ToDictionary(a => a.folder, a => a.folderOffset);
                var folderFileBlockOffsets = new Dictionary<BsaFolder, uint>();
                var fileRecordOffsets = new Dictionary<BethesdaFile, uint>();
                var fileDataSizes = new Dictionary<BethesdaFile, uint>();
                var fileDataOffsets = new Dictionary<BethesdaFile, uint>();

                var offset = (uint)writer.BaseStream.Position;
                foreach (var folder in _bsa)
                {
                    var folderFilesOffset = offset + header.TotalFileNameLength;
                    folderFileBlockOffsets.Add(folder, folderFilesOffset);

                    if (namedDirectories)
                    {
                        writer.WriteBZString(folder.Path);
                    }

                    foreach (var file in folder)
                    {
                        fileRecordOffsets.Add(file, (uint)writer.BaseStream.Position);
                        var fileRecord = new FileRecord
                        {
                            hash = file.Hash
                        };

                        writer.Write(fileRecord.hash);
                        writer.Seek(sizeof(uint), SeekOrigin.Current);
                        writer.Seek(sizeof(uint), SeekOrigin.Current);
                    }

                    offset = (uint)writer.BaseStream.Position;
                }

                allFileNames.ForEach(writer.WriteCString);
                offset = (uint)writer.BaseStream.Position;

                var folders = folderRecordOffsets
                    .Zip(folderFileBlockOffsets, (a, b) => new { folderRecordOffset = (uint)a.Value + Bsa.SizeRecordOffset, offsetValue = b.Value });
                foreach (var folder in folders)
                {
                    writer.BaseStream.Seek(folder.folderRecordOffset, SeekOrigin.Begin);
                    writer.Write(folder.offsetValue);
                }

                var bstringPrefixed = header.ArchiveFlags.HasFlag(ArchiveFlags.BStringPrefixed);

                writer.BaseStream.Seek(offset, SeekOrigin.Begin);
                foreach (var file in allFiles)
                {
                    var fileDataOffset = (uint)writer.BaseStream.Position;
                    fileDataOffsets.Add(file, fileDataOffset);

                    uint size;

                    if (bstringPrefixed)
                    {
                        writer.WriteBString(file.Filename);
                    }
                    if (file.IsCompressFlagSet ^ defaultCompress)
                    {
                        var beginOffset = (uint)writer.BaseStream.Position;
                        //write compressed
                        writer.Write((uint)file.Data.Length);
                        var zlib = new Zlib();
                        using (var dataStream = new MemoryStream(file.Data))
                        using (var deflateStream = zlib.CompressStream(writer.BaseStream, System.IO.Compression.CompressionLevel.Optimal))
                        {
                            dataStream.CopyTo(deflateStream);
                        }
                        size = (uint)(writer.BaseStream.Position - beginOffset);
                    }
                    else
                    {
                        //write normal
                        writer.Write(file.Data);
                        size = (uint)file.Data.Length;
                    }

                    if (file.IsCompressFlagSet)
                    {
                        size |= BethesdaFile.FlagCompress;
                    }

                    fileDataSizes.Add(file, size);
                }

                var files = fileRecordOffsets
                    .Zip(fileDataOffsets, (a, b) => new { file = a.Key, fileRecordOffset = a.Value, offsetValue = b.Value })
                    .Zip(fileDataSizes, (a, b) => new { a.file, a.fileRecordOffset, a.offsetValue, size = b.Value });
                foreach (var file in files)
                {
                    writer.BaseStream.Seek(file.fileRecordOffset + sizeof(ulong), SeekOrigin.Begin);
                    writer.Write(file.size);
                    writer.Write(file.offsetValue);
                }
            }
        }
#endif

        public static FileFlags CreateFileFlags(IEnumerable<string> allFiles)
        {
            FileFlags flags = 0;

            //take extension of each bsafile name, take distinct, convert to uppercase
            var extSet = new HashSet<string>(
                allFiles
                .Select(Path.GetExtension)
                .Select(ext => ext.ToUpperInvariant())
            );

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
