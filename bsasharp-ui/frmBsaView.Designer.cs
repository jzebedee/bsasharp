namespace bsasharp_ui
{
    partial class frmBsaView
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.splitContainer = new System.Windows.Forms.SplitContainer();
            this.treeBsa = new BrightIdeasSoftware.TreeListView();
            this.olvColumnPath = ((BrightIdeasSoftware.OLVColumn)(new BrightIdeasSoftware.OLVColumn()));
            this.olvColumnSize = ((BrightIdeasSoftware.OLVColumn)(new BrightIdeasSoftware.OLVColumn()));
            this.olvColumnCompression = ((BrightIdeasSoftware.OLVColumn)(new BrightIdeasSoftware.OLVColumn()));
            this.contextMenuStripBsaTree = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.unpackToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.deleteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.uncompressedToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.level1ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.level2ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.level3ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.level4ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.level5ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.level6ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.level7ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.level8ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.level9ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.flpControls = new System.Windows.Forms.FlowLayoutPanel();
            this.grpFilter = new System.Windows.Forms.GroupBox();
            this.txtFilter = new System.Windows.Forms.TextBox();
            this.ttipBsa = new System.Windows.Forms.ToolTip(this.components);
            this.dlgOpenBsa = new System.Windows.Forms.OpenFileDialog();
            this.btnSaveBsa = new System.Windows.Forms.Button();
            this.dlgSaveBsa = new System.Windows.Forms.SaveFileDialog();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).BeginInit();
            this.splitContainer.Panel1.SuspendLayout();
            this.splitContainer.Panel2.SuspendLayout();
            this.splitContainer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.treeBsa)).BeginInit();
            this.contextMenuStripBsaTree.SuspendLayout();
            this.flpControls.SuspendLayout();
            this.grpFilter.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitContainer
            // 
            this.splitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer.Location = new System.Drawing.Point(0, 0);
            this.splitContainer.Name = "splitContainer";
            // 
            // splitContainer.Panel1
            // 
            this.splitContainer.Panel1.Controls.Add(this.treeBsa);
            // 
            // splitContainer.Panel2
            // 
            this.splitContainer.Panel2.Controls.Add(this.flpControls);
            this.splitContainer.Size = new System.Drawing.Size(784, 561);
            this.splitContainer.SplitterDistance = 615;
            this.splitContainer.TabIndex = 1;
            // 
            // treeBsa
            // 
            this.treeBsa.AllColumns.Add(this.olvColumnPath);
            this.treeBsa.AllColumns.Add(this.olvColumnSize);
            this.treeBsa.AllColumns.Add(this.olvColumnCompression);
            this.treeBsa.AlternateRowBackColor = System.Drawing.Color.GhostWhite;
            this.treeBsa.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.olvColumnPath,
            this.olvColumnSize,
            this.olvColumnCompression});
            this.treeBsa.ContextMenuStrip = this.contextMenuStripBsaTree;
            this.treeBsa.Cursor = System.Windows.Forms.Cursors.Default;
            this.treeBsa.Dock = System.Windows.Forms.DockStyle.Fill;
            this.treeBsa.FullRowSelect = true;
            this.treeBsa.HeaderUsesThemes = false;
            this.treeBsa.Location = new System.Drawing.Point(0, 0);
            this.treeBsa.Name = "treeBsa";
            this.treeBsa.OwnerDraw = true;
            this.treeBsa.ShowCommandMenuOnRightClick = true;
            this.treeBsa.ShowGroups = false;
            this.treeBsa.ShowImagesOnSubItems = true;
            this.treeBsa.Size = new System.Drawing.Size(615, 561);
            this.treeBsa.TabIndex = 1;
            this.treeBsa.UseAlternatingBackColors = true;
            this.treeBsa.UseCompatibleStateImageBehavior = false;
            this.treeBsa.UseFilterIndicator = true;
            this.treeBsa.UseFiltering = true;
            this.treeBsa.UseHotItem = true;
            this.treeBsa.UseTranslucentHotItem = true;
            this.treeBsa.UseTranslucentSelection = true;
            this.treeBsa.View = System.Windows.Forms.View.Details;
            this.treeBsa.VirtualMode = true;
            // 
            // olvColumnPath
            // 
            this.olvColumnPath.AspectName = "Text";
            this.olvColumnPath.CellPadding = null;
            this.olvColumnPath.FillsFreeSpace = true;
            this.olvColumnPath.Text = "Path";
            // 
            // olvColumnSize
            // 
            this.olvColumnSize.AspectName = "Size";
            this.olvColumnSize.AspectToStringFormat = "";
            this.olvColumnSize.CellPadding = null;
            this.olvColumnSize.FillsFreeSpace = true;
            this.olvColumnSize.MaximumWidth = 200;
            this.olvColumnSize.MinimumWidth = 80;
            this.olvColumnSize.Text = "Size";
            this.olvColumnSize.Width = 80;
            // 
            // olvColumnCompression
            // 
            this.olvColumnCompression.AspectName = "CompressionLevel";
            this.olvColumnCompression.CellPadding = null;
            this.olvColumnCompression.Text = "Compression";
            this.olvColumnCompression.UseFiltering = false;
            this.olvColumnCompression.Width = 80;
            // 
            // contextMenuStripBsaTree
            // 
            this.contextMenuStripBsaTree.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.unpackToolStripMenuItem,
            this.deleteToolStripMenuItem,
            this.toolStripMenuItem1});
            this.contextMenuStripBsaTree.Name = "contextMenuStripBsaTree";
            this.contextMenuStripBsaTree.Size = new System.Drawing.Size(145, 70);
            // 
            // unpackToolStripMenuItem
            // 
            this.unpackToolStripMenuItem.Name = "unpackToolStripMenuItem";
            this.unpackToolStripMenuItem.Size = new System.Drawing.Size(144, 22);
            this.unpackToolStripMenuItem.Text = "Unpack";
            // 
            // deleteToolStripMenuItem
            // 
            this.deleteToolStripMenuItem.Name = "deleteToolStripMenuItem";
            this.deleteToolStripMenuItem.Size = new System.Drawing.Size(144, 22);
            this.deleteToolStripMenuItem.Text = "Delete";
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.uncompressedToolStripMenuItem,
            this.level1ToolStripMenuItem,
            this.level2ToolStripMenuItem,
            this.level3ToolStripMenuItem,
            this.level4ToolStripMenuItem,
            this.level5ToolStripMenuItem,
            this.level6ToolStripMenuItem,
            this.level7ToolStripMenuItem,
            this.level8ToolStripMenuItem,
            this.level9ToolStripMenuItem});
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(144, 22);
            this.toolStripMenuItem1.Text = "Compression";
            // 
            // uncompressedToolStripMenuItem
            // 
            this.uncompressedToolStripMenuItem.Name = "uncompressedToolStripMenuItem";
            this.uncompressedToolStripMenuItem.Size = new System.Drawing.Size(157, 22);
            this.uncompressedToolStripMenuItem.Text = "Uncompressed";
            // 
            // level1ToolStripMenuItem
            // 
            this.level1ToolStripMenuItem.Name = "level1ToolStripMenuItem";
            this.level1ToolStripMenuItem.Size = new System.Drawing.Size(157, 22);
            this.level1ToolStripMenuItem.Text = "Level 1 (Fastest)";
            // 
            // level2ToolStripMenuItem
            // 
            this.level2ToolStripMenuItem.Name = "level2ToolStripMenuItem";
            this.level2ToolStripMenuItem.Size = new System.Drawing.Size(157, 22);
            this.level2ToolStripMenuItem.Text = "Level 2";
            // 
            // level3ToolStripMenuItem
            // 
            this.level3ToolStripMenuItem.Name = "level3ToolStripMenuItem";
            this.level3ToolStripMenuItem.Size = new System.Drawing.Size(157, 22);
            this.level3ToolStripMenuItem.Text = "Level 3";
            // 
            // level4ToolStripMenuItem
            // 
            this.level4ToolStripMenuItem.Name = "level4ToolStripMenuItem";
            this.level4ToolStripMenuItem.Size = new System.Drawing.Size(157, 22);
            this.level4ToolStripMenuItem.Text = "Level 4";
            // 
            // level5ToolStripMenuItem
            // 
            this.level5ToolStripMenuItem.Name = "level5ToolStripMenuItem";
            this.level5ToolStripMenuItem.Size = new System.Drawing.Size(157, 22);
            this.level5ToolStripMenuItem.Text = "Level 5";
            // 
            // level6ToolStripMenuItem
            // 
            this.level6ToolStripMenuItem.Name = "level6ToolStripMenuItem";
            this.level6ToolStripMenuItem.Size = new System.Drawing.Size(157, 22);
            this.level6ToolStripMenuItem.Text = "Level 6";
            // 
            // level7ToolStripMenuItem
            // 
            this.level7ToolStripMenuItem.Name = "level7ToolStripMenuItem";
            this.level7ToolStripMenuItem.Size = new System.Drawing.Size(157, 22);
            this.level7ToolStripMenuItem.Text = "Level 7";
            // 
            // level8ToolStripMenuItem
            // 
            this.level8ToolStripMenuItem.Name = "level8ToolStripMenuItem";
            this.level8ToolStripMenuItem.Size = new System.Drawing.Size(157, 22);
            this.level8ToolStripMenuItem.Text = "Level 8";
            // 
            // level9ToolStripMenuItem
            // 
            this.level9ToolStripMenuItem.Name = "level9ToolStripMenuItem";
            this.level9ToolStripMenuItem.Size = new System.Drawing.Size(157, 22);
            this.level9ToolStripMenuItem.Text = "Level 9 (Best)";
            // 
            // flpControls
            // 
            this.flpControls.Controls.Add(this.grpFilter);
            this.flpControls.Controls.Add(this.btnSaveBsa);
            this.flpControls.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flpControls.Location = new System.Drawing.Point(0, 0);
            this.flpControls.MinimumSize = new System.Drawing.Size(100, 200);
            this.flpControls.Name = "flpControls";
            this.flpControls.Size = new System.Drawing.Size(165, 561);
            this.flpControls.TabIndex = 0;
            // 
            // grpFilter
            // 
            this.grpFilter.Controls.Add(this.txtFilter);
            this.grpFilter.Location = new System.Drawing.Point(3, 3);
            this.grpFilter.Name = "grpFilter";
            this.grpFilter.Size = new System.Drawing.Size(156, 40);
            this.grpFilter.TabIndex = 8;
            this.grpFilter.TabStop = false;
            this.grpFilter.Text = "Filter";
            // 
            // txtFilter
            // 
            this.txtFilter.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            this.txtFilter.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.CustomSource;
            this.txtFilter.CharacterCasing = System.Windows.Forms.CharacterCasing.Lower;
            this.txtFilter.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtFilter.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtFilter.Location = new System.Drawing.Point(3, 16);
            this.txtFilter.Name = "txtFilter";
            this.txtFilter.Size = new System.Drawing.Size(150, 20);
            this.txtFilter.TabIndex = 8;
            this.txtFilter.TextChanged += new System.EventHandler(this.txtFilter_TextChanged);
            // 
            // dlgOpenBsa
            // 
            this.dlgOpenBsa.DefaultExt = "bsa";
            this.dlgOpenBsa.Filter = "Bethesda Archive|*.bsa";
            // 
            // btnSaveBsa
            // 
            this.btnSaveBsa.Location = new System.Drawing.Point(3, 49);
            this.btnSaveBsa.Name = "btnSaveBsa";
            this.btnSaveBsa.Size = new System.Drawing.Size(75, 23);
            this.btnSaveBsa.TabIndex = 9;
            this.btnSaveBsa.Text = "Save BSA";
            this.btnSaveBsa.UseVisualStyleBackColor = true;
            this.btnSaveBsa.Click += new System.EventHandler(this.btnSaveBsa_Click);
            // 
            // dlgSaveBsa
            // 
            this.dlgSaveBsa.DefaultExt = "bsa";
            this.dlgSaveBsa.Filter = "Bethesda Archive|*.bsa";
            // 
            // frmBsaView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(784, 561);
            this.Controls.Add(this.splitContainer);
            this.Name = "frmBsaView";
            this.Text = "BSA Viewer";
            this.splitContainer.Panel1.ResumeLayout(false);
            this.splitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).EndInit();
            this.splitContainer.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.treeBsa)).EndInit();
            this.contextMenuStripBsaTree.ResumeLayout(false);
            this.flpControls.ResumeLayout(false);
            this.grpFilter.ResumeLayout(false);
            this.grpFilter.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainer;
        private System.Windows.Forms.ToolTip ttipBsa;
        private System.Windows.Forms.OpenFileDialog dlgOpenBsa;
        private BrightIdeasSoftware.TreeListView treeBsa;
        private BrightIdeasSoftware.OLVColumn olvColumnPath;
        private BrightIdeasSoftware.OLVColumn olvColumnSize;
        private System.Windows.Forms.FlowLayoutPanel flpControls;
        private BrightIdeasSoftware.OLVColumn olvColumnCompression;
        private System.Windows.Forms.GroupBox grpFilter;
        private System.Windows.Forms.TextBox txtFilter;
        private System.Windows.Forms.ContextMenuStrip contextMenuStripBsaTree;
        private System.Windows.Forms.ToolStripMenuItem unpackToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem deleteToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem uncompressedToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem level1ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem level2ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem level3ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem level4ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem level5ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem level6ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem level7ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem level8ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem level9ToolStripMenuItem;
        private System.Windows.Forms.Button btnSaveBsa;
        private System.Windows.Forms.SaveFileDialog dlgSaveBsa;

    }
}

