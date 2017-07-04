using BSAsharp;
using BSAsharp.Extensions;
using BSAsharp.Format;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace bsasharp
{
    public class BsaWriter
    {
        private Dictionary<BsaFolder, uint> _folderRecordOffsetsA, _folderRecordOffsetsB;
        private Dictionary<BethesdaFile, uint> _fileRecordOffsetsA, _fileRecordOffsetsB;

        public void Save(string path, Bsa bsa, BsaHeader header)
        {
            using (var stream = File.OpenWrite(path))
                Save(stream, bsa, header);
        }

        public void Save(Stream stream, Bsa bsa, BsaHeader header)
        {
            var allFiles = bsa.SelectMany(fold => fold).ToList();
            var allFileNames = allFiles.Select(file => file.Name).ToList();

            _folderRecordOffsetsA = new Dictionary<BsaFolder, uint>(bsa.Count);
            _folderRecordOffsetsB = new Dictionary<BsaFolder, uint>(bsa.Count);

            _fileRecordOffsetsA = new Dictionary<BethesdaFile, uint>(allFiles.Count);
            _fileRecordOffsetsB = new Dictionary<BethesdaFile, uint>(allFiles.Count);

            header.Field = Bsa.BsaGreet;
            header.Version = Bsa.FalloutVersion;
            header.Offset = BsaHeader.Size;
            header.FolderCount = (uint)bsa.Count;
            header.FileCount = (uint)allFileNames.Count;
            header.TotalFolderNameLength = (uint)bsa.Sum(folder => folder.Path.Length + 1);
            header.TotalFileNameLength = (uint)allFileNames.Sum(file => file.Length + 1);

            using (var writer = new BinaryWriter(stream))
            {
                header.Write(writer);

                foreach (var folder in bsa)
                    WriteFolderRecord(writer, folder);

                foreach (var folder in bsa)
                    WriteFileRecordBlock(writer, folder, header.TotalFileNameLength);

                allFileNames.ForEach(writer.WriteCString);

                var defaultCompress = header.ArchiveFlags.HasFlag(ArchiveFlags.DefaultCompressed);
                var bstringPrefixed = header.ArchiveFlags.HasFlag(ArchiveFlags.BStringPrefixed);
                allFiles.ForEach(file => WriteFileBlock(writer, file, defaultCompress, bstringPrefixed)); 

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

        private void WriteFileBlock(BinaryWriter writer, BethesdaFile file, bool defaultCompress, bool bstringPrefixed)
        {
            _fileRecordOffsetsB.Add(file, (uint)writer.BaseStream.Position);
            if (bstringPrefixed)
            {
                writer.WriteBString(file.Filename);
            }
            if (file.IsCompressFlagSet ^ defaultCompress)
            {
                //write compressed
                writer.Write((uint)file.Data.Length + 4);
                var zlib = new Zlib();
                using (var dataStream = new MemoryStream(file.Data))
                using (var deflateStream = zlib.CompressStream(writer.BaseStream, System.IO.Compression.CompressionLevel.Optimal))
                {
                    dataStream.CopyTo(deflateStream);
                }
            }
            else
            {
                //write normal
                writer.Write(file.Data);
            }
        }

        private void WriteFolderRecord(BinaryWriter writer, BsaFolder folder)
        {
            _folderRecordOffsetsA.Add(folder, (uint)writer.BaseStream.Position + Bsa.SizeRecordOffset);
            folder.Record.Write(writer);
        }

        private void WriteFileRecordBlock(BinaryWriter writer, BsaFolder folder, uint totalFileNameLength)
        {
            _folderRecordOffsetsB.Add(folder, (uint)writer.BaseStream.Position + totalFileNameLength);
            writer.WriteBZString(folder.Path);

            foreach (var file in folder)
            {
                _fileRecordOffsetsA.Add(file, (uint)writer.BaseStream.Position + Bsa.SizeRecordOffset);
                var fileRecord = new FileRecord
                {
                    hash = file.Hash,
                    size = (uint)file.Data.Length
                };
                fileRecord.Write(writer);
            }
        }

        private FileFlags CreateFileFlags(IEnumerable<string> allFiles)
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
