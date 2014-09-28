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
            //Must be all lower case, and use backslash as directory delimiter
            Path = Util.FixPath(path);
            Hash = Util.CreateHash(Path, "");
        }
        private BsaFolder(IEnumerable<BsaFile> collection)
            : base(collection ?? new SortedSet<BsaFile>(), BsaHashComparer.Instance)
        {
        }

        public void Unpack(string outFolder)
        {
            var outPath = System.IO.Path.Combine(outFolder, Path);
            if (!Directory.Exists(outPath))
                Directory.CreateDirectory(outPath);

            foreach (var file in this)
            {
                var filePath = System.IO.Path.Combine(outFolder, file.Filename);
                File.WriteAllBytes(filePath, file.GetContents(true));
            }
        }

        public override string ToString()
        {
            return Path;
        }
    }
}
