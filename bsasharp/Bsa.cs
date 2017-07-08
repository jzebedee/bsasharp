using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Threading.Tasks;
using BSAsharp.Extensions;
using BSAsharp.Progress;
using BSAsharp.Format;

namespace BSAsharp
{
    public class Bsa : SortedSet<BsaFolder>, IDisposable
    {
        internal const int
            FalloutVersion = 0x68,
            BsaMagic = 0x415342; //'B','S','A','\0'
        internal const uint BsaMaxSize = 0x80000000; //2 GiB

        private readonly BsaReader _bsaReader;

        public BsaHeader Header => _bsaReader.Header;

        /// <summary>
        /// Creates a new BSAWrapper instance around an existing BSA file
        /// </summary>
        /// <param name="bsaPath">The path of the file to open</param>
        /// <param name="options"></param>
        public Bsa(string bsaPath)
            : this(MemoryMappedFile.CreateFromFile(bsaPath, FileMode.Open, null, 0L, MemoryMappedFileAccess.Read))
        {
        }

        /// <summary>
        /// Creates an empty BSAWrapper instance that can be modified and saved to a BSA file
        /// </summary>
        public Bsa() : base(BsaHashComparer.Instance)
        {
        }

        //wtf C#
        //please get real ctor overloads someday
        private Bsa(MemoryMappedFile bsaMap) : this(new BsaReader(bsaMap))
        {
        }

        private Bsa(BsaReader bsaReader) : base(bsaReader.Read(), BsaHashComparer.Instance)
        {
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
                _bsaReader?.Dispose();
            }
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Pack(string packFolder)
        {
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
                    folder.Add(new BethesdaFile(g.Key, Path.GetFileName(file), File.ReadAllBytes(file)));
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
    }
}