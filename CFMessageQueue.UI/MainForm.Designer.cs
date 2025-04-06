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
            tscbQueue = new ToolStripComboBox();
            dgvQueueMessage = new DataGridView();
            toolStrip1.SuspendLayout();
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
            toolStrip1.Items.AddRange(new ToolStripItem[] { tscbQueue });
            toolStrip1.Location = new Point(0, 0);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Size = new Size(881, 25);
            toolStrip1.TabIndex = 1;
            toolStrip1.Text = "toolStrip1";
            // 
            // tscbQueue
            // 
            tscbQueue.DropDownStyle = ComboBoxStyle.DropDownList;
            tscbQueue.Name = "tscbQueue";
            tscbQueue.Size = new Size(121, 25);
            tscbQueue.SelectedIndexChanged += tscbQueue_SelectedIndexChanged;
            // 
            // dgvQueueMessage
            // 
            dgvQueueMessage.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvQueueMessage.Dock = DockStyle.Fill;
            dgvQueueMessage.Location = new Point(0, 25);
            dgvQueueMessage.Name = "dgvQueueMessage";
            dgvQueueMessage.Size = new Size(881, 451);
            dgvQueueMessage.TabIndex = 2;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(881, 498);
            Controls.Add(dgvQueueMessage);
            Controls.Add(toolStrip1);
            Controls.Add(statusStrip1);
            Name = "MainForm";
            Text = "Form1";
            Load += MainForm_Load;
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dgvQueueMessage).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private StatusStrip statusStrip1;
        private ToolStrip toolStrip1;
        private ToolStripComboBox tscbQueue;
        private DataGridView dgvQueueMessage;
    }
}
