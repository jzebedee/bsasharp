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
            this.treeBsa = new System.Windows.Forms.TreeView();
            this.ttipBsa = new System.Windows.Forms.ToolTip(this.components);
            this.dlgOpenBsa = new System.Windows.Forms.OpenFileDialog();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).BeginInit();
            this.splitContainer.Panel1.SuspendLayout();
            this.splitContainer.SuspendLayout();
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
            this.splitContainer.Size = new System.Drawing.Size(784, 561);
            this.splitContainer.SplitterDistance = 353;
            this.splitContainer.TabIndex = 1;
            // 
            // treeBsa
            // 
            this.treeBsa.Dock = System.Windows.Forms.DockStyle.Fill;
            this.treeBsa.Location = new System.Drawing.Point(0, 0);
            this.treeBsa.Name = "treeBsa";
            this.treeBsa.Size = new System.Drawing.Size(353, 561);
            this.treeBsa.TabIndex = 1;
            // 
            // dlgOpenBsa
            // 
            this.dlgOpenBsa.DefaultExt = "bsa";
            this.dlgOpenBsa.Filter = "Bethesda Archive|*.bsa";
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
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).EndInit();
            this.splitContainer.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainer;
        private System.Windows.Forms.TreeView treeBsa;
        private System.Windows.Forms.ToolTip ttipBsa;
        private System.Windows.Forms.OpenFileDialog dlgOpenBsa;

    }
}

