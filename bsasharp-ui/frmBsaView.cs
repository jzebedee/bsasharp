using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using BrightIdeasSoftware;
using BSAsharp;

namespace bsasharp_ui
{
    public partial class frmBsaView : Form
    {
        private BsaTree _tree;

        public frmBsaView()
        {
            InitializeComponent();

            var testBsa = new Bsa(@"X:\Storage\WorkTTW\release-2\release-2\resources\TTW Data\TTW Files\Main Files\TaleOfTwoWastelands.bsa");
            _tree = new BsaTree(testBsa);

            foreach (var text in _tree.Root.Descendants.Select(node => node.Text).Distinct())
                txtFilter.AutoCompleteCustomSource.Add(text);

            treeBsa.CanExpandGetter = (dynamic model) => model.Count > 0;
            treeBsa.ChildrenGetter = (dynamic model) => model;

            olvColumnSize.AspectToStringConverter = (dynamic model, dynamic value) => model.SizeText;

            treeBsa.BeginUpdate();
            treeBsa.SetObjects(_tree.Root);
            treeBsa.EndUpdate();

            //dlgOpenBsa.ShowDialog();
        }

        private void SetFilter()
        {
            treeBsa.ModelFilter = new ModelFilter(p =>
            {
                var filter = txtFilter.Text;
                if (string.IsNullOrWhiteSpace(filter))
                    return true;

                var node = p as BsaNode;
                if (node == null)
                    return true;

                var matches = _tree.FileNameTrie.Retrieve(filter);
                return node.DescendantsAndSelf.Any(subnode => matches.Contains(subnode.Text));
            });
        }

        private void txtFilter_TextChanged(object sender, EventArgs e)
        {
            SetFilter();
        }
    }
}
