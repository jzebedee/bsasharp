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
        public List<BSAFile> Children { get; private set; }

        public BSAFolder(string path, IEnumerable<BSAFile> children)
            : this(path)
        {
            this.Children = children.ToList();
        }
        public BSAFolder(string path)
        {
            //Must be all lower case, and use backslash as directory delimiter
            this.Path = path.ToLowerInvariant().Replace('/', '\\');
            this.Children = new List<BSAFile>();
        }

        public void AddFile(BSAFile newFile)
        {
            Children.Add(newFile);
        }
    }
}
