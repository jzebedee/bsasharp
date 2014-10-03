using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using BSAsharp;

namespace bsasharp_ui
{
    public partial class frmBsaView : Form
    {
        public frmBsaView()
        {
            InitializeComponent();

            var testBsa = new Bsa(@"X:\Storage\WorkTTW\release-2\release-2\resources\TTW Data\TTW Files\Main Files\TaleOfTwoWastelands.bsa");
            var bsaTree = new BsaTree(testBsa);
            treeBsa.Tag = testBsa;

            var bsaFiles = from file in testBsa.SelectMany(folder => folder)
                           group file by file.Path.Split('\\');

            var bsaFolders =
                from folder in testBsa
                group folder by folder.Path.Split('\\');

            treeBsa.Nodes.AddRange(testBsa.Select(folder => new TreeNode(folder.Path, folder.Count == 0 ? Enumerable.Empty<TreeNode>().ToArray() : new[] { new TreeNode() })).ToArray());
        }

        private void treeBsa_AfterExpand(dynamic sender, TreeViewEventArgs e)
        {
            var bsa = sender.Tag;
            Console.WriteLine(e);
        }
    }
}
