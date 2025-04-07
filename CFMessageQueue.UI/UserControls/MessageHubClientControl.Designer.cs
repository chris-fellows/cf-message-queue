namespace CFMessageQueue.UI.UserControls
{
    partial class MessageHubClientControl
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
            label1 = new Label();
            txtName = new TextBox();
            txtSecurityKey = new TextBox();
            label2 = new Label();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(11, 13);
            label1.Name = "label1";
            label1.Size = new Size(42, 15);
            label1.TabIndex = 0;
            label1.Text = "Name:";
            // 
            // txtName
            // 
            txtName.Location = new Point(69, 11);
            txtName.Name = "txtName";
            txtName.Size = new Size(358, 23);
            txtName.TabIndex = 1;
            // 
            // txtSecurityKey
            // 
            txtSecurityKey.Location = new Point(69, 50);
            txtSecurityKey.Name = "txtSecurityKey";
            txtSecurityKey.Size = new Size(358, 23);
            txtSecurityKey.TabIndex = 3;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(11, 52);
            label2.Name = "label2";
            label2.Size = new Size(29, 15);
            label2.TabIndex = 2;
            label2.Text = "Key:";
            // 
            // MessageHubClientControl
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(txtSecurityKey);
            Controls.Add(label2);
            Controls.Add(txtName);
            Controls.Add(label1);
            Name = "MessageHubClientControl";
            Size = new Size(534, 236);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label label1;
        private TextBox txtName;
        private TextBox txtSecurityKey;
        private Label label2;
    }
}
