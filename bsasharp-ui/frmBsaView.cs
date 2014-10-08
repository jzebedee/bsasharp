using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Dynamic;
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
            var testTree = new BsaTree(testBsa);

            treeBsa.CanExpandGetter = (dynamic model) => model.Count > 0;
            treeBsa.ChildrenGetter = (dynamic model) => model;

            olvColumnSize.AspectToStringConverter = (dynamic model, dynamic value) => model.SizeText;

            treeBsa.BeginUpdate();
            treeBsa.SetObjects(testTree.Root);
            treeBsa.EndUpdate();

            //dlgOpenBsa.ShowDialog();
        }
    }
}
