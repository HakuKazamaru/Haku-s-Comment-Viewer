namespace HakuCommentViewer.WinForm
{
    partial class BootForm
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
            lbTitle = new Label();
            lbStatus = new Label();
            progressBar1 = new ProgressBar();
            SuspendLayout();
            // 
            // lbTitle
            // 
            lbTitle.AutoSize = true;
            lbTitle.Font = new Font("Yu Gothic UI", 72F, FontStyle.Regular, GraphicsUnit.Point);
            lbTitle.ForeColor = SystemColors.ControlLightLight;
            lbTitle.Location = new Point(28, 116);
            lbTitle.Name = "lbTitle";
            lbTitle.Size = new Size(737, 159);
            lbTitle.TabIndex = 0;
            lbTitle.Text = "コメントビュワー";
            // 
            // lbStatus
            // 
            lbStatus.AutoSize = true;
            lbStatus.Font = new Font("Yu Gothic UI", 18F, FontStyle.Regular, GraphicsUnit.Point);
            lbStatus.ForeColor = SystemColors.ControlLightLight;
            lbStatus.Location = new Point(565, 400);
            lbStatus.Name = "lbStatus";
            lbStatus.Size = new Size(223, 41);
            lbStatus.TabIndex = 1;
            lbStatus.Text = "よみこみちゅう・・・";
            // 
            // progressBar1
            // 
            progressBar1.Location = new Point(12, 368);
            progressBar1.Name = "progressBar1";
            progressBar1.Size = new Size(776, 29);
            progressBar1.Style = ProgressBarStyle.Marquee;
            progressBar1.TabIndex = 2;
            // 
            // BootForm
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = SystemColors.ControlDarkDark;
            ClientSize = new Size(800, 450);
            Controls.Add(progressBar1);
            Controls.Add(lbStatus);
            Controls.Add(lbTitle);
            FormBorderStyle = FormBorderStyle.None;
            MaximumSize = new Size(800, 450);
            MinimizeBox = false;
            MinimumSize = new Size(800, 450);
            Name = "BootForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "起動中・・・";
            TopMost = true;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label lbTitle;
        private Label lbStatus;
        private ProgressBar progressBar1;
    }
}