using System.Linq;
using BSAsharp;

namespace bsasharp_ui
{
    class BsaTree
    {
        private readonly Bsa _bsa;

        public BsaNode Root { get; private set; }

        public BsaTree(Bsa bsa)
        {
            _bsa = bsa;
            Root = new BsaNode();
            CreateStructure();
        }

        private void CreateStructure()
        {
            foreach (var folder in _bsa)
            {
                var current = Root;

                var splitPath = folder.Path.Split('\\');
                foreach (var chunk in splitPath)
                {
                    //previousExpando = currentExpando;
                    if (current.All(node => node.Text != chunk))
                        current.Add((current = new BsaNode { Text = chunk }));
                }

                current.AddRange(folder.Select(file => new BsaNode
                {
                    Text = file.Name, Tag = file, Size = (int) file.OriginalSize
                }));
            }
        }
    }
}
