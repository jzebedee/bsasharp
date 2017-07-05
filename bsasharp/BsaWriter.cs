using BSAsharp;
using BSAsharp.Extensions;
using BSAsharp.Format;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
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
            using (var mmf = MemoryMappedFile.CreateFromFile(path, FileMode.Create, null, Bsa.BsaMaxSize, MemoryMappedFileAccess.ReadWrite))
            {
                SaveUnsafe(mmf, header);
            }
        }

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
                offset += FolderRecord.Size * folderRecords.Length;
            }

            var namedDirectories = header.ArchiveFlags.HasFlag(ArchiveFlags.NamedDirectories);
            var defaultCompress = header.ArchiveFlags.HasFlag(ArchiveFlags.DefaultCompressed);

            var folderRecordOffsets = _bsa
                .Select((folder, i) => new { folder, folderOffset = header.Offset + (FolderRecord.Size * i) })
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

            var folders = folderRecordOffsets.OrderBy(a => a.Key.Hash)
                .Zip(folderFileBlockOffsets.OrderBy(a => a.Key.Hash), (a, b) => new { folderRecordOffset = (uint)a.Value + Bsa.SizeRecordOffset, offsetValue = b.Value });
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

            var files = fileRecordOffsets.OrderBy(a => a.Key.Hash)
                .Zip(fileDataOffsets.OrderBy(a => a.Key.Hash), (a, b) => new { file = a.Key, fileRecordOffset = a.Value, offsetValue = b.Value })
                .Zip(fileDataSizes.OrderBy(a => a.Key.Hash), (a, b) => new { a.file, a.fileRecordOffset, a.offsetValue, size = b.Value });
            using (var acc = mmf.CreateViewAccessor())
            {
                foreach (var file in files)
                {
                    unsafe
                    {
                        try
                        {
                            byte* ptr = (byte*)0;
                            acc.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);

                            var fileRecord = (FileRecord*)(ptr + file.fileRecordOffset);
                            fileRecord->size = file.size;
                            fileRecord->offset = file.offsetValue;

                            byte[] data = new byte[(file.size & ~BethesdaFile.FlagCompress) - sizeof(uint)]; 
                            var originalSize = acc.ReadUInt32(file.offsetValue);
                            System.Diagnostics.Debug.Assert(acc.ReadArray<byte>(file.offsetValue + sizeof(uint), data, 0, data.Length) == data.Length);

                            var zlib = new Zlib();
                            var inflatedData = zlib.Decompress(new MemoryStream(data));
                            System.Diagnostics.Debug.Assert(inflatedData.Length == originalSize);
                        }
                        finally
                        {
                            acc.SafeMemoryMappedViewHandle.ReleasePointer();
                        }
                    }
                }
            }

        }

        public void Save(Stream stream, BsaHeader header)
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

            using (var writer = new BinaryWriter(stream))
            {
                header.Write(writer);

                //write all folder records and get back FolderRecord*[]
                var folderRecordOffsets = _bsa.Select(folder => WriteFolderRecord(writer, folder)).ToArray();

                var namedDirectories = header.ArchiveFlags.HasFlag(ArchiveFlags.NamedDirectories);
                var defaultCompress = header.ArchiveFlags.HasFlag(ArchiveFlags.DefaultCompressed);

                //write all folder names and file record blocks, get back { folder, FileRecord*[] for folder, and FileRecordBlock* }
                //FileRecordBlock* must be written back into FolderRecord*
                var fileRecordBlocks = _bsa.Select(folder =>
                 {
                     var fileOffsets = WriteFileRecordBlock(writer, folder, namedDirectories, defaultCompress, header.TotalFileNameLength, out long folderFilesOffset);
                     return new { folder, folderFilesOffset, fileOffsets };
                 }).ToArray();

                allFileNames.ForEach(writer.WriteCString);

                //_fileRecordOffsetsB
                var bstringPrefixed = header.ArchiveFlags.HasFlag(ArchiveFlags.BStringPrefixed);
                var fileDataOffsets = allFiles.Select(file => WriteFileBlock(writer, file, defaultCompress, bstringPrefixed)).ToArray();

                var folderMap = folderRecordOffsets
                   .Zip(fileRecordBlocks, (folderRecordOffset, firA) => new { firA.folder, folderRecordOffset, firA.folderFilesOffset, firA.fileOffsets });

                int i = 0;
                foreach (var a in folderMap)
                {
                    //skip to folderRecord + [fieldOffset] for offset
                    writer.BaseStream.Seek(a.folderRecordOffset + Bsa.SizeRecordOffset, SeekOrigin.Begin);
                    writer.Write(a.folderFilesOffset);

                    foreach (var fileOffset in a.fileOffsets)
                    {
                        //skip to fileRecord + [fieldOffset] for size
                        writer.BaseStream.Seek(fileOffset + sizeof(ulong), SeekOrigin.Begin);

                        var fileData = fileDataOffsets[i++];
                        writer.Write(fileData.Value);
                        writer.Write(fileData.Key);
                    }
                }
            }
        }

        private KeyValuePair<uint, uint> WriteFileBlock(BinaryWriter writer, BethesdaFile file, bool defaultCompress, bool bstringPrefixed)
        {
            var fileDataOffset = (uint)writer.BaseStream.Position;
            uint size;

            if (bstringPrefixed)
            {
                writer.WriteBString(file.Filename);
            }
            if (file.IsCompressFlagSet ^ defaultCompress)
            {
                //write compressed
                writer.Write((uint)file.Data.Length);
                var zlib = new Zlib();
                using (var dataStream = new MemoryStream(file.Data))
                using (var deflateStream = zlib.CompressStream(writer.BaseStream, System.IO.Compression.CompressionLevel.Optimal))
                {
                    dataStream.CopyTo(deflateStream);
                    //set size = compressed size + sizeof(originalSize field)
                    size = (uint)writer.BaseStream.Position + sizeof(uint);
                }
            }
            else
            {
                //write normal
                writer.Write(file.Data);
                size = (uint)file.Data.Length;
            }

            return new KeyValuePair<uint, uint>(fileDataOffset, size);
        }

        private long WriteFolderRecord(BinaryWriter writer, BsaFolder folder)
        {
            var ret = writer.BaseStream.Position;
            folder.Record.Write(writer);
            return ret;
        }

        private List<long> WriteFileRecordBlock(BinaryWriter writer, BsaFolder folder, bool namedDirectories, bool defaultCompress, uint totalFileNameLength, out long folderFilesOffset)
        {
            folderFilesOffset = writer.BaseStream.Position + totalFileNameLength;
            if (namedDirectories)
            {
                writer.WriteBZString(folder.Path);
            }

            var fileOffsets = new List<long>(folder.Count);
            foreach (var file in folder)
            {
                fileOffsets.Add(writer.BaseStream.Position);
                var fileRecord = new FileRecord
                {
                    hash = file.Hash
                };
                fileRecord.Write(writer);
            }

            return fileOffsets;
        }

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
