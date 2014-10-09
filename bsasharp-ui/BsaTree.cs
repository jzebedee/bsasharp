using System.Linq;
using BSAsharp;
using Gma.DataStructures.StringSearch;

namespace bsasharp_ui
{
    public class BsaTree
    {
        private readonly Bsa _bsa;

        public PatriciaSuffixTrie<BsaNode> FileNameTrie { get; private set; }

        public BsaNode Root { get; private set; }

        public BsaTree(Bsa bsa)
        {
            _bsa = bsa;
            FileNameTrie = new PatriciaSuffixTrie<BsaNode>(1);
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
                    {
                        current.Add((current = new BsaNode { Text = chunk }));
                        FileNameTrie.Add(chunk, current);
                    }
                }

                foreach (var file in folder)
                {
                    var fileNode = new BsaNode
                    {
                        Text = file.Name,
                        Tag = file,
                        Size = (int) file.OriginalSize
                    };

                    FileNameTrie.Add(file.Name, fileNode);
                    current.Add(fileNode);
                }
            }
        }
    }
}
