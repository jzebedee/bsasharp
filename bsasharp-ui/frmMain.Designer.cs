namespace BSAsharp_ui
{
    partial class frmMain
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
            this.tscMain = new System.Windows.Forms.ToolStripContainer();
            this.staFileInfo = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.splDualFiletree = new System.Windows.Forms.SplitContainer();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.toolStrip3 = new System.Windows.Forms.ToolStrip();
            this.tscMain.BottomToolStripPanel.SuspendLayout();
            this.tscMain.ContentPanel.SuspendLayout();
            this.tscMain.TopToolStripPanel.SuspendLayout();
            this.tscMain.SuspendLayout();
            this.staFileInfo.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splDualFiletree)).BeginInit();
            this.splDualFiletree.SuspendLayout();
            this.SuspendLayout();
            // 
            // tscMain
            // 
            // 
            // tscMain.BottomToolStripPanel
            // 
            this.tscMain.BottomToolStripPanel.Controls.Add(this.staFileInfo);
            // 
            // tscMain.ContentPanel
            // 
            this.tscMain.ContentPanel.Controls.Add(this.splDualFiletree);
            this.tscMain.ContentPanel.Size = new System.Drawing.Size(584, 395);
            this.tscMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tscMain.Location = new System.Drawing.Point(0, 0);
            this.tscMain.Name = "tscMain";
            this.tscMain.Size = new System.Drawing.Size(584, 441);
            this.tscMain.TabIndex = 0;
            this.tscMain.Text = "toolStripContainer1";
            // 
            // tscMain.TopToolStripPanel
            // 
            this.tscMain.TopToolStripPanel.Controls.Add(this.menuStrip1);
            // 
            // staFileInfo
            // 
            this.staFileInfo.Dock = System.Windows.Forms.DockStyle.None;
            this.staFileInfo.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel1});
            this.staFileInfo.Location = new System.Drawing.Point(0, 0);
            this.staFileInfo.Name = "staFileInfo";
            this.staFileInfo.Size = new System.Drawing.Size(584, 22);
            this.staFileInfo.TabIndex = 0;
            // 
            // toolStripStatusLabel1
            // 
            this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            this.toolStripStatusLabel1.Size = new System.Drawing.Size(118, 17);
            this.toolStripStatusLabel1.Text = "toolStripStatusLabel1";
            // 
            // splDualFiletree
            // 
            this.splDualFiletree.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splDualFiletree.Location = new System.Drawing.Point(0, 0);
            this.splDualFiletree.Name = "splDualFiletree";
            this.splDualFiletree.Size = new System.Drawing.Size(584, 395);
            this.splDualFiletree.SplitterDistance = 292;
            this.splDualFiletree.TabIndex = 0;
            // 
            // menuStrip1
            // 
            this.menuStrip1.Dock = System.Windows.Forms.DockStyle.None;
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(584, 24);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // toolStrip3
            // 
            this.toolStrip3.Location = new System.Drawing.Point(0, 0);
            this.toolStrip3.Name = "toolStrip3";
            this.toolStrip3.Size = new System.Drawing.Size(584, 25);
            this.toolStrip3.TabIndex = 1;
            this.toolStrip3.Text = "toolStrip3";
            // 
            // frmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(584, 441);
            this.Controls.Add(this.toolStrip3);
            this.Controls.Add(this.tscMain);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "frmMain";
            this.Text = "BSA# Editor";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.frmMain_FormClosed);
            this.Load += new System.EventHandler(this.frmMain_Load);
            this.tscMain.BottomToolStripPanel.ResumeLayout(false);
            this.tscMain.BottomToolStripPanel.PerformLayout();
            this.tscMain.ContentPanel.ResumeLayout(false);
            this.tscMain.TopToolStripPanel.ResumeLayout(false);
            this.tscMain.TopToolStripPanel.PerformLayout();
            this.tscMain.ResumeLayout(false);
            this.tscMain.PerformLayout();
            this.staFileInfo.ResumeLayout(false);
            this.staFileInfo.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splDualFiletree)).EndInit();
            this.splDualFiletree.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ToolStripContainer tscMain;
        private System.Windows.Forms.SplitContainer splDualFiletree;
        private System.Windows.Forms.StatusStrip staFileInfo;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStrip toolStrip3;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel1;

    }
}

