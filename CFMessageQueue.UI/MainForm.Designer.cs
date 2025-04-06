namespace CFMessageQueue.UI
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            statusStrip1 = new StatusStrip();
            toolStrip1 = new ToolStrip();
            splitContainer1 = new SplitContainer();
            tvwNodes = new TreeView();
            dgvQueueMessage = new DataGridView();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvQueueMessage).BeginInit();
            SuspendLayout();
            // 
            // statusStrip1
            // 
            statusStrip1.Location = new Point(0, 476);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Size = new Size(881, 22);
            statusStrip1.TabIndex = 0;
            statusStrip1.Text = "statusStrip1";
            // 
            // toolStrip1
            // 
            toolStrip1.Location = new Point(0, 0);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Size = new Size(881, 25);
            toolStrip1.TabIndex = 1;
            toolStrip1.Text = "toolStrip1";
            // 
            // splitContainer1
            // 
            splitContainer1.Dock = DockStyle.Fill;
            splitContainer1.Location = new Point(0, 25);
            splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(tvwNodes);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(dgvQueueMessage);
            splitContainer1.Size = new Size(881, 451);
            splitContainer1.SplitterDistance = 293;
            splitContainer1.TabIndex = 2;
            // 
            // tvwNodes
            // 
            tvwNodes.Dock = DockStyle.Fill;
            tvwNodes.Location = new Point(0, 0);
            tvwNodes.Name = "tvwNodes";
            tvwNodes.Size = new Size(293, 451);
            tvwNodes.TabIndex = 0;
            tvwNodes.AfterSelect += tvwNodes_AfterSelect;
            // 
            // dgvQueueMessage
            // 
            dgvQueueMessage.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvQueueMessage.Dock = DockStyle.Fill;
            dgvQueueMessage.Location = new Point(0, 0);
            dgvQueueMessage.MultiSelect = false;
            dgvQueueMessage.Name = "dgvQueueMessage";
            dgvQueueMessage.ReadOnly = true;
            dgvQueueMessage.RowHeadersVisible = false;
            dgvQueueMessage.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvQueueMessage.Size = new Size(584, 451);
            dgvQueueMessage.TabIndex = 0;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(881, 498);
            Controls.Add(splitContainer1);
            Controls.Add(toolStrip1);
            Controls.Add(statusStrip1);
            Name = "MainForm";
            Text = "Form1";
            Load += MainForm_Load;
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvQueueMessage).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private StatusStrip statusStrip1;
        private ToolStrip toolStrip1;
        private SplitContainer splitContainer1;
        private TreeView tvwNodes;
        private DataGridView dgvQueueMessage;
    }
}
