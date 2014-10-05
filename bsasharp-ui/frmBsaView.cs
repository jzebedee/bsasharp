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

            dlgOpenBsa.ShowDialog();
        }
    }
}
