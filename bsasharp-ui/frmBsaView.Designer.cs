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
            this.treeBsa = new System.Windows.Forms.TreeView();
            this.SuspendLayout();
            // 
            // treeBsa
            // 
            this.treeBsa.Dock = System.Windows.Forms.DockStyle.Fill;
            this.treeBsa.Location = new System.Drawing.Point(0, 0);
            this.treeBsa.Name = "treeBsa";
            this.treeBsa.Size = new System.Drawing.Size(284, 261);
            this.treeBsa.TabIndex = 0;
            this.treeBsa.AfterExpand += new System.Windows.Forms.TreeViewEventHandler(this.treeBsa_AfterExpand);
            // 
            // frmBsaView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 261);
            this.Controls.Add(this.treeBsa);
            this.Name = "frmBsaView";
            this.Text = "BSA Viewer";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TreeView treeBsa;
    }
}

