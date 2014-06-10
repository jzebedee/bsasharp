using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BSAsharp
{
    /// <summary>
    /// A managed representation of a BSA folder.
    /// </summary>
    public class BSAFolder
    {
        public string Path { get; private set; }
        public IEnumerable<BSAFile> Children { get; private set; }

        public BSAFolder(string path, IEnumerable<BSAFile> children)
        {
            this.Path = path;
            this.Children = children;
        }
    }
}
