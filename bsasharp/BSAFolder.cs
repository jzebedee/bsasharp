using System;
using System.Threading;
using System.Threading.Tasks;
using BSAsharp.Extensions;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

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
            if(string.IsNullOrEmpty(path))
                throw new ArgumentException("Folder must have a path");

            //Must be all lower case, and use backslash as directory delimiter
            Path = Util.FixPath(path);
            Hash = Util.CreateHash(Path);
        }
        private BsaFolder(IEnumerable<BsaFile> collection)
            : base(collection ?? new SortedSet<BsaFile>(), BsaHashComparer.Instance)
        {
        }

        public void Unpack(string outFolder)
        {
            EnsureDirectory(outFolder);
            foreach (var file in this)
            {
                var outFilepath = System.IO.Path.Combine(outFolder, file.Filename);
                using (Stream
                    outFilestream = File.OpenWrite(outFilepath),
                    bsaFilestream = file.GetContentStream(true))
                {
                    bsaFilestream.CopyTo(outFilestream);
                }
            }
        }
        public IEnumerable<Task> UnpackAsync(string outFolder, int bufferSize = 4096, CancellationToken? token = null)
        {
            EnsureDirectory(outFolder);
            foreach (var file in this)
            {
                var outFilepath = System.IO.Path.Combine(outFolder, file.Filename);
                using (Stream
                    outFilestream = File.OpenWrite(outFilepath),
                    bsaFilestream = file.GetContentStream(true))
                {
                    yield return bsaFilestream.CopyToAsync(outFilestream, bufferSize, token ?? CancellationToken.None);
                }
            }
        }

        private void EnsureDirectory(string outFolder)
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
