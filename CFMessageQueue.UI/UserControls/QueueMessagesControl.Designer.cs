namespace CFMessageQueue.UI.UserControls
{
    partial class QueueMessagesControl
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(QueueMessagesControl));
            toolStrip1 = new ToolStrip();
            tsbPrevPage = new ToolStripButton();
            tslPage = new ToolStripLabel();
            tsbNextPage = new ToolStripButton();
            dgvQueueMessage = new DataGridView();
            toolStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvQueueMessage).BeginInit();
            SuspendLayout();
            // 
            // toolStrip1
            // 
            toolStrip1.Items.AddRange(new ToolStripItem[] { tsbPrevPage, tslPage, tsbNextPage });
            toolStrip1.Location = new Point(0, 0);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Size = new Size(794, 25);
            toolStrip1.TabIndex = 0;
            toolStrip1.Text = "toolStrip1";
            // 
            // tsbPrevPage
            // 
            tsbPrevPage.Image = (Image)resources.GetObject("tsbPrevPage.Image");
            tsbPrevPage.ImageTransparentColor = Color.Magenta;
            tsbPrevPage.Name = "tsbPrevPage";
            tsbPrevPage.Size = new Size(43, 22);
            tsbPrevPage.Text = "<<";
            tsbPrevPage.Click += tsbPrevPage_Click;
            // 
            // tslPage
            // 
            tslPage.Name = "tslPage";
            tslPage.Size = new Size(42, 22);
            tslPage.Text = "Page 1";
            // 
            // tsbNextPage
            // 
            tsbNextPage.Image = (Image)resources.GetObject("tsbNextPage.Image");
            tsbNextPage.ImageTransparentColor = Color.Magenta;
            tsbNextPage.Name = "tsbNextPage";
            tsbNextPage.Size = new Size(43, 22);
            tsbNextPage.Text = ">>";
            tsbNextPage.Click += tsbNextPage_Click;
            // 
            // dgvQueueMessage
            // 
            dgvQueueMessage.AllowUserToAddRows = false;
            dgvQueueMessage.AllowUserToDeleteRows = false;
            dgvQueueMessage.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvQueueMessage.Dock = DockStyle.Fill;
            dgvQueueMessage.Location = new Point(0, 25);
            dgvQueueMessage.Name = "dgvQueueMessage";
            dgvQueueMessage.ReadOnly = true;
            dgvQueueMessage.RowHeadersVisible = false;
            dgvQueueMessage.Size = new Size(794, 473);
            dgvQueueMessage.TabIndex = 1;
            // 
            // QueueMessagesControl
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(dgvQueueMessage);
            Controls.Add(toolStrip1);
            Name = "QueueMessagesControl";
            Size = new Size(794, 498);
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dgvQueueMessage).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private ToolStrip toolStrip1;
        private DataGridView dgvQueueMessage;
        private ToolStripButton tsbNextPage;
        private ToolStripButton tsbPrevPage;
        private ToolStripLabel tslPage;
    }
}
