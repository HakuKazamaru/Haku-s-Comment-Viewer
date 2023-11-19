namespace HakuCommentViewer.WinForm
{
    partial class MainForm
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
            components = new System.ComponentModel.Container();
            wv2Main = new Microsoft.Web.WebView2.WinForms.WebView2();
            tmrWebSrvCheck = new System.Windows.Forms.Timer(components);
            lbLoadText = new Label();
            ((System.ComponentModel.ISupportInitialize)wv2Main).BeginInit();
            SuspendLayout();
            // 
            // wv2Main
            // 
            wv2Main.AllowExternalDrop = false;
            wv2Main.CreationProperties = null;
            wv2Main.DefaultBackgroundColor = Color.White;
            wv2Main.Dock = DockStyle.Fill;
            wv2Main.Location = new Point(0, 0);
            wv2Main.Name = "wv2Main";
            wv2Main.Size = new Size(1262, 673);
            wv2Main.TabIndex = 0;
            wv2Main.ZoomFactor = 1D;
            wv2Main.NavigationCompleted += wv2Main_NavigationCompleted;
            // 
            // tmrWebSrvCheck
            // 
            tmrWebSrvCheck.Tick += tmrWebSrvCheck_Tick;
            // 
            // lbLoadText
            // 
            lbLoadText.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            lbLoadText.AutoSize = true;
            lbLoadText.BackColor = SystemColors.WindowFrame;
            lbLoadText.Font = new Font("Yu Gothic UI", 72F, FontStyle.Regular, GraphicsUnit.Point);
            lbLoadText.ForeColor = SystemColors.Window;
            lbLoadText.Location = new Point(204, 256);
            lbLoadText.Name = "lbLoadText";
            lbLoadText.Size = new Size(881, 159);
            lbLoadText.TabIndex = 2;
            lbLoadText.Text = "よみこみちゅう・・・";
            lbLoadText.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = SystemColors.WindowFrame;
            ClientSize = new Size(1262, 673);
            Controls.Add(wv2Main);
            Controls.Add(lbLoadText);
            ForeColor = SystemColors.Window;
            MinimumSize = new Size(1280, 720);
            Name = "MainForm";
            Text = "コメントビュワー";
            Load += MainForm_Load;
            ((System.ComponentModel.ISupportInitialize)wv2Main).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Microsoft.Web.WebView2.WinForms.WebView2 wv2Main;
        private System.Windows.Forms.Timer tmrWebSrvCheck;
        private Label lbLoadText;
    }
}