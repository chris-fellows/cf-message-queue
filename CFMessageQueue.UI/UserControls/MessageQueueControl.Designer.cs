namespace CFMessageQueue.UI.UserControls
{
    partial class MessageQueueControl
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
            lblQueueMaxProcessing = new Label();
            label4 = new Label();
            lblQueueMaxSize = new Label();
            label3 = new Label();
            lblQueueName = new Label();
            label1 = new Label();
            dgvClient = new DataGridView();
            label2 = new Label();
            ((System.ComponentModel.ISupportInitialize)dgvClient).BeginInit();
            SuspendLayout();
            // 
            // lblQueueMaxProcessing
            // 
            lblQueueMaxProcessing.AutoSize = true;
            lblQueueMaxProcessing.Location = new Point(181, 74);
            lblQueueMaxProcessing.Name = "lblQueueMaxProcessing";
            lblQueueMaxProcessing.Size = new Size(52, 15);
            lblQueueMaxProcessing.TabIndex = 13;
            lblQueueMaxProcessing.Text = "<None>";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(19, 74);
            label4.Name = "label4";
            label4.Size = new Size(156, 15);
            label4.TabIndex = 12;
            label4.Text = "Max Concurrent Processing:";
            // 
            // lblQueueMaxSize
            // 
            lblQueueMaxSize.AutoSize = true;
            lblQueueMaxSize.Location = new Point(81, 47);
            lblQueueMaxSize.Name = "lblQueueMaxSize";
            lblQueueMaxSize.Size = new Size(52, 15);
            lblQueueMaxSize.TabIndex = 11;
            lblQueueMaxSize.Text = "<None>";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(19, 47);
            label3.Name = "label3";
            label3.Size = new Size(56, 15);
            label3.TabIndex = 10;
            label3.Text = "Max Size:";
            // 
            // lblQueueName
            // 
            lblQueueName.AutoSize = true;
            lblQueueName.Location = new Point(67, 21);
            lblQueueName.Name = "lblQueueName";
            lblQueueName.Size = new Size(52, 15);
            lblQueueName.TabIndex = 9;
            lblQueueName.Text = "<None>";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(19, 21);
            label1.Name = "label1";
            label1.Size = new Size(42, 15);
            label1.TabIndex = 8;
            label1.Text = "Name:";
            // 
            // dgvClient
            // 
            dgvClient.AllowUserToAddRows = false;
            dgvClient.AllowUserToDeleteRows = false;
            dgvClient.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dgvClient.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvClient.Location = new Point(19, 131);
            dgvClient.Name = "dgvClient";
            dgvClient.ReadOnly = true;
            dgvClient.RowHeadersVisible = false;
            dgvClient.Size = new Size(555, 322);
            dgvClient.TabIndex = 14;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(21, 109);
            label2.Name = "label2";
            label2.Size = new Size(46, 15);
            label2.TabIndex = 15;
            label2.Text = "Clients:";
            // 
            // MessageQueueControl
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(label2);
            Controls.Add(dgvClient);
            Controls.Add(lblQueueMaxProcessing);
            Controls.Add(label4);
            Controls.Add(lblQueueMaxSize);
            Controls.Add(label3);
            Controls.Add(lblQueueName);
            Controls.Add(label1);
            Name = "MessageQueueControl";
            Size = new Size(845, 480);
            ((System.ComponentModel.ISupportInitialize)dgvClient).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label lblQueueMaxProcessing;
        private Label label4;
        private Label lblQueueMaxSize;
        private Label label3;
        private Label lblQueueName;
        private Label label1;
        private DataGridView dgvClient;
        private Label label2;
    }
}
