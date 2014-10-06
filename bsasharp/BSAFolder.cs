using System;
using System.Threading;
using System.Threading.Tasks;
using BSAsharp.Extensions;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using BSAsharp.Progress;

namespace BSAsharp
{
    /// <summary>
    /// A managed representation of a BSA folder.
    /// </summary>
    [DebuggerDisplay("{Path} ({Count})")]
    public class BsaFolder : SortedSet<BsaFile>, IBsaEntry
    {
        public string Path { get; private set; }

        public ulong Hash { get; private set; }

        public BsaFolder(string path, IEnumerable<BsaFile> children = null)
            : this(children)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Folder must have a path");

            //Must be all lower case, and use backslash as directory delimiter
            Path = Util.FixPath(path);
            Hash = Util.CreateHash(Path);
        }
        private BsaFolder(IEnumerable<BsaFile> collection)
            : base(collection ?? new SortedSet<BsaFile>(), BsaHashComparer.Instance)
        {
        }

        /// <summary>
        /// Saves every BsaFile in this BsaFolder to the directory specified by outFolder
        /// </summary>
        /// <param name="outFolder">The directory to place unpacked files</param>
        public void Unpack(string outFolder)
        {
            EnsureDirectoryExists(outFolder);
            foreach (var file in this)
            {
                var outFilepath = System.IO.Path.Combine(outFolder, file.Filename);
                using (Stream
                    outFilestream = File.OpenWrite(outFilepath),
                    bsaFilestream = file.GetContentStream(true))
                    bsaFilestream.CopyTo(outFilestream);
            }
        }
        /// <summary>
        /// Asynchronously saves every BsaFile in this BsaFolder to the directory specified by outFolder, with support for progress and cancellation
        /// </summary>
        /// <param name="outFolder">The directory to place unpacked files</param>
        /// <param name="progress">An object to receive progress updates, or null</param>
        /// <param name="token">A CancellationToken used to end the unpack operation prematurely, or null</param>
        /// <returns></returns>
        public async Task UnpackAsync(string outFolder, IProgress<UnpackProgress> progress = null, /*int bufferSize = 4096, */CancellationToken? token = null)
        {
            EnsureDirectoryExists(outFolder);
            var setToken = token ?? CancellationToken.None;
            var count = 0;

            foreach (var file in this)
            //Parallel.ForEach(this, new ParallelOptions { CancellationToken = setToken }, async file =>
            {
                if (setToken.IsCancellationRequested)
                    return;

                var outFilepath = System.IO.Path.Combine(outFolder, file.Filename);
                using (Stream
                    outFilestream = File.OpenWrite(outFilepath),
                    bsaFilestream = file.GetContentStream(true))
                {
                    await bsaFilestream.CopyToAsync(outFilestream, 0x1000/*default*/, setToken);
                    if (progress != null)
                        progress.Report(new UnpackProgress(file.Filename, ++count, Count));
                }
            }
        }

        private void EnsureDirectoryExists(string outFolder)
        {
            var outPath = System.IO.Path.Combine(outFolder, Path);
            if (!Directory.Exists(outPath))
                Directory.CreateDirectory(outPath);
        }

        public override string ToString()
        {
            return Path;
        }
    }
}
