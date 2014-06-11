using BSAsharp.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace BSAsharp
{
    /// <summary>
    /// A managed representation of a BSA folder.
    /// </summary>
    public class BSAFolder : SortedSet<BSAFile>, IHashed
    {
        public string Path { get; private set; }

        private readonly Lazy<ulong> _hash;
        public ulong Hash { get { return _hash.Value; } }

        public BSAFolder(string path, IEnumerable<BSAFile> children = null)
            : this(children ?? new SortedSet<BSAFile>())
        {
            //Must be all lower case, and use backslash as directory delimiter
            this.Path = path.ToLowerInvariant().Replace('/', '\\');
            this._hash = new Lazy<ulong>(() => Util.CreateHash(Path, ""), true);
        }

        private BSAFolder(IEnumerable<BSAFile> collection)
            : base(collection, HashComparer.Instance)
        {
        }
    }
}