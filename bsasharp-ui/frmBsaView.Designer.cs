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
            this.ttipBsa = new System.Windows.Forms.ToolTip(this.components);
            this.dlgOpenBsa = new System.Windows.Forms.OpenFileDialog();
            this.olvColumnPath = ((BrightIdeasSoftware.OLVColumn)(new BrightIdeasSoftware.OLVColumn()));
            this.olvColumnSize = ((BrightIdeasSoftware.OLVColumn)(new BrightIdeasSoftware.OLVColumn()));
            this.flpControls = new System.Windows.Forms.FlowLayoutPanel();
            this.button1 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.button3 = new System.Windows.Forms.Button();
            this.button4 = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).BeginInit();
            this.splitContainer.Panel1.SuspendLayout();
            this.splitContainer.Panel2.SuspendLayout();
            this.splitContainer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.treeBsa)).BeginInit();
            this.flpControls.SuspendLayout();
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
            this.splitContainer.SplitterDistance = 553;
            this.splitContainer.TabIndex = 1;
            // 
            // treeBsa
            // 
            this.treeBsa.AllColumns.Add(this.olvColumnPath);
            this.treeBsa.AllColumns.Add(this.olvColumnSize);
            this.treeBsa.AlternateRowBackColor = System.Drawing.Color.GhostWhite;
            this.treeBsa.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.olvColumnPath,
            this.olvColumnSize});
            this.treeBsa.Dock = System.Windows.Forms.DockStyle.Fill;
            this.treeBsa.HeaderUsesThemes = false;
            this.treeBsa.Location = new System.Drawing.Point(0, 0);
            this.treeBsa.Name = "treeBsa";
            this.treeBsa.OwnerDraw = true;
            this.treeBsa.ShowGroups = false;
            this.treeBsa.Size = new System.Drawing.Size(553, 561);
            this.treeBsa.TabIndex = 1;
            this.treeBsa.UseAlternatingBackColors = true;
            this.treeBsa.UseCompatibleStateImageBehavior = false;
            this.treeBsa.UseExplorerTheme = true;
            this.treeBsa.UseFilterIndicator = true;
            this.treeBsa.UseFiltering = true;
            this.treeBsa.View = System.Windows.Forms.View.Details;
            this.treeBsa.VirtualMode = true;
            // 
            // dlgOpenBsa
            // 
            this.dlgOpenBsa.DefaultExt = "bsa";
            this.dlgOpenBsa.Filter = "Bethesda Archive|*.bsa";
            // 
            // olvColumnPath
            // 
            this.olvColumnPath.AspectName = "Key";
            this.olvColumnPath.CellPadding = null;
            this.olvColumnPath.FillsFreeSpace = true;
            this.olvColumnPath.Text = "Path";
            // 
            // olvColumnSize
            // 
            this.olvColumnSize.AspectName = "Value.SizeText";
            this.olvColumnSize.CellPadding = null;
            this.olvColumnSize.FillsFreeSpace = true;
            this.olvColumnSize.MaximumWidth = 200;
            this.olvColumnSize.MinimumWidth = 80;
            this.olvColumnSize.Text = "Size";
            this.olvColumnSize.Width = 80;
            // 
            // flpControls
            // 
            this.flpControls.Controls.Add(this.button1);
            this.flpControls.Controls.Add(this.button2);
            this.flpControls.Controls.Add(this.button3);
            this.flpControls.Controls.Add(this.button4);
            this.flpControls.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flpControls.Location = new System.Drawing.Point(0, 0);
            this.flpControls.Name = "flpControls";
            this.flpControls.Size = new System.Drawing.Size(227, 561);
            this.flpControls.TabIndex = 0;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(3, 3);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 0;
            this.button1.Text = "sexyA";
            this.button1.UseVisualStyleBackColor = true;
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(84, 3);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(75, 23);
            this.button2.TabIndex = 1;
            this.button2.Text = "sexyB";
            this.button2.UseVisualStyleBackColor = true;
            // 
            // button3
            // 
            this.button3.Location = new System.Drawing.Point(3, 32);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(75, 23);
            this.button3.TabIndex = 2;
            this.button3.Text = "sexyC";
            this.button3.UseVisualStyleBackColor = true;
            // 
            // button4
            // 
            this.button4.Location = new System.Drawing.Point(84, 32);
            this.button4.Name = "button4";
            this.button4.Size = new System.Drawing.Size(75, 23);
            this.button4.TabIndex = 3;
            this.button4.Text = "sexyD";
            this.button4.UseVisualStyleBackColor = true;
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
            this.flpControls.ResumeLayout(false);
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
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Button button3;
        private System.Windows.Forms.Button button4;

    }
}

