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

            treeBsa.Nodes.AddRange(bsaTree.Nodes.ToArray());
        }

        private void treeBsa_AfterExpand(object sender, TreeViewEventArgs e)
        {
            var expando = e.Node.Tag as IDictionary<string, object>;
            var nodes = e.Node.Nodes;
            nodes.RemoveByKey("_");

            if (expando != null)
            {
                foreach (var kvp in expando)
                {
                    var newNode = new TreeNode(kvp.Key) { Tag = kvp.Value };
                    if (kvp.Value is IDictionary<string, object>)
                        newNode.Nodes.Add("_", "");
                    nodes.Add(newNode);
                }
                e.Node.Tag = null;
            }
        }
    }
}
