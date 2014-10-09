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
        private readonly BsaTree _tree;
        private readonly Bsa _bsa;

        public frmBsaView()
        {
            InitializeComponent();

#if DEBUG
            _bsa = new Bsa(@"X:\Storage\WorkTTW\release-2\release-2\resources\TTW Data\TTW Files\Main Files\TaleOfTwoWastelands.bsa");
#else
            switch (dlgOpenBsa.ShowDialog())
            {
                case DialogResult.OK:
                    _bsa = new Bsa(dlgOpenBsa.FileName);
                    break;
                default:
                    Environment.Exit(1);
                    return;
            }
#endif

            Debug.Assert(_bsa != null);
            _tree = new BsaTree(_bsa);

            foreach (var text in _tree.Root.Descendants.Select(node => node.Text).Distinct())
                txtFilter.AutoCompleteCustomSource.Add(text);

            treeBsa.CanExpandGetter = (dynamic model) => model.Count > 0;
            treeBsa.ChildrenGetter = (dynamic model) => model;

            olvColumnSize.AspectToStringConverter = (dynamic model, dynamic value) => model.SizeText;

            treeBsa.BeginUpdate();
            treeBsa.SetObjects(_tree.Root);
            treeBsa.EndUpdate();
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
                var matchesEtc = matches.SelectMany(match => match.DescendantsAndSelf);
                return node.DescendantsAndSelf.Any(matchesEtc.Contains);
            });
        }

        private void txtFilter_TextChanged(object sender, EventArgs e)
        {
            SetFilter();
        }

        private void btnSaveBsa_Click(object sender, EventArgs e)
        {
            switch (dlgSaveBsa.ShowDialog())
            {
                case DialogResult.OK:
                    _bsa.Save(dlgSaveBsa.FileName, true);
                    MessageBox.Show("BSA saved");
                    break;
                default:
                    break;
            }
        }
    }
}
