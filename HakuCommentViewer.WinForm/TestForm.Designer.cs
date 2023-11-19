namespace HakuCommentViewer.WinForm
{
    partial class TestForm
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
            components = new System.ComponentModel.Container();
            msMenuBar = new MenuStrip();
            tsmiFile = new ToolStripMenuItem();
            tsmiClose = new ToolStripMenuItem();
            ssStatusBar = new StatusStrip();
            tsslStatus = new ToolStripStatusLabel();
            commentWatchTimer = new System.Windows.Forms.Timer(components);
            mainPanel = new Panel();
            mainSplitContainer = new SplitContainer();
            dgvStreamManager = new DataGridView();
            lbStreamManager = new Label();
            subPanel = new Panel();
            bottomSplitContainer = new SplitContainer();
            dgvCommentViewer = new DataGridView();
            lbCommentViewer = new Label();
            dgvGift = new DataGridView();
            lbGift = new Label();
            msMenuBar.SuspendLayout();
            ssStatusBar.SuspendLayout();
            mainPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)mainSplitContainer).BeginInit();
            mainSplitContainer.Panel1.SuspendLayout();
            mainSplitContainer.Panel2.SuspendLayout();
            mainSplitContainer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvStreamManager).BeginInit();
            subPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)bottomSplitContainer).BeginInit();
            bottomSplitContainer.Panel1.SuspendLayout();
            bottomSplitContainer.Panel2.SuspendLayout();
            bottomSplitContainer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvCommentViewer).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dgvGift).BeginInit();
            SuspendLayout();
            // 
            // msMenuBar
            // 
            msMenuBar.ImageScalingSize = new Size(20, 20);
            msMenuBar.Items.AddRange(new ToolStripItem[] { tsmiFile });
            msMenuBar.Location = new Point(0, 0);
            msMenuBar.Name = "msMenuBar";
            msMenuBar.Padding = new Padding(6, 3, 0, 3);
            msMenuBar.Size = new Size(800, 30);
            msMenuBar.TabIndex = 1;
            msMenuBar.Text = "menuStrip1";
            // 
            // tsmiFile
            // 
            tsmiFile.DropDownItems.AddRange(new ToolStripItem[] { tsmiClose });
            tsmiFile.Name = "tsmiFile";
            tsmiFile.Size = new Size(86, 24);
            tsmiFile.Text = "ファイル (&F)";
            // 
            // tsmiClose
            // 
            tsmiClose.Name = "tsmiClose";
            tsmiClose.Size = new Size(152, 26);
            tsmiClose.Text = "閉じる (&C)";
            tsmiClose.Click += tsmiClose_Click;
            // 
            // ssStatusBar
            // 
            ssStatusBar.ImageScalingSize = new Size(20, 20);
            ssStatusBar.Items.AddRange(new ToolStripItem[] { tsslStatus });
            ssStatusBar.Location = new Point(0, 425);
            ssStatusBar.Name = "ssStatusBar";
            ssStatusBar.Size = new Size(800, 26);
            ssStatusBar.TabIndex = 2;
            ssStatusBar.Text = "statusStrip1";
            // 
            // tsslStatus
            // 
            tsslStatus.Name = "tsslStatus";
            tsslStatus.Size = new Size(69, 20);
            tsslStatus.Text = "未初期化";
            // 
            // commentWatchTimer
            // 
            commentWatchTimer.Enabled = true;
            commentWatchTimer.Tick += commentWatchTimer_Tick;
            // 
            // mainPanel
            // 
            mainPanel.Controls.Add(mainSplitContainer);
            mainPanel.Dock = DockStyle.Fill;
            mainPanel.Location = new Point(0, 30);
            mainPanel.Name = "mainPanel";
            mainPanel.Size = new Size(800, 395);
            mainPanel.TabIndex = 3;
            // 
            // mainSplitContainer
            // 
            mainSplitContainer.Dock = DockStyle.Fill;
            mainSplitContainer.Location = new Point(0, 0);
            mainSplitContainer.Name = "mainSplitContainer";
            mainSplitContainer.Orientation = Orientation.Horizontal;
            // 
            // mainSplitContainer.Panel1
            // 
            mainSplitContainer.Panel1.Controls.Add(dgvStreamManager);
            mainSplitContainer.Panel1.Controls.Add(lbStreamManager);
            // 
            // mainSplitContainer.Panel2
            // 
            mainSplitContainer.Panel2.Controls.Add(subPanel);
            mainSplitContainer.Size = new Size(800, 395);
            mainSplitContainer.SplitterDistance = 152;
            mainSplitContainer.TabIndex = 0;
            // 
            // dgvStreamManager
            // 
            dgvStreamManager.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvStreamManager.Dock = DockStyle.Fill;
            dgvStreamManager.Location = new Point(0, 20);
            dgvStreamManager.Name = "dgvStreamManager";
            dgvStreamManager.RowHeadersWidth = 51;
            dgvStreamManager.RowTemplate.Height = 29;
            dgvStreamManager.Size = new Size(800, 132);
            dgvStreamManager.TabIndex = 1;
            dgvStreamManager.CellContentClick += dgvStreamManager_CellContentClick;
            dgvStreamManager.RowsAdded += dgvStreamManager_RowsAdded;
            // 
            // lbStreamManager
            // 
            lbStreamManager.AutoSize = true;
            lbStreamManager.Dock = DockStyle.Top;
            lbStreamManager.Location = new Point(0, 0);
            lbStreamManager.Name = "lbStreamManager";
            lbStreamManager.Size = new Size(101, 20);
            lbStreamManager.TabIndex = 0;
            lbStreamManager.Text = "・ストリーム管理";
            // 
            // subPanel
            // 
            subPanel.Controls.Add(bottomSplitContainer);
            subPanel.Dock = DockStyle.Fill;
            subPanel.Location = new Point(0, 0);
            subPanel.Name = "subPanel";
            subPanel.Size = new Size(800, 239);
            subPanel.TabIndex = 0;
            // 
            // bottomSplitContainer
            // 
            bottomSplitContainer.Dock = DockStyle.Fill;
            bottomSplitContainer.Location = new Point(0, 0);
            bottomSplitContainer.Name = "bottomSplitContainer";
            // 
            // bottomSplitContainer.Panel1
            // 
            bottomSplitContainer.Panel1.Controls.Add(dgvCommentViewer);
            bottomSplitContainer.Panel1.Controls.Add(lbCommentViewer);
            bottomSplitContainer.Panel1MinSize = 50;
            // 
            // bottomSplitContainer.Panel2
            // 
            bottomSplitContainer.Panel2.Controls.Add(dgvGift);
            bottomSplitContainer.Panel2.Controls.Add(lbGift);
            bottomSplitContainer.Panel2MinSize = 50;
            bottomSplitContainer.Size = new Size(800, 239);
            bottomSplitContainer.SplitterDistance = 390;
            bottomSplitContainer.SplitterWidth = 5;
            bottomSplitContainer.TabIndex = 0;
            // 
            // dgvCommentViewer
            // 
            dgvCommentViewer.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvCommentViewer.Dock = DockStyle.Fill;
            dgvCommentViewer.Location = new Point(0, 20);
            dgvCommentViewer.Name = "dgvCommentViewer";
            dgvCommentViewer.ReadOnly = true;
            dgvCommentViewer.RowHeadersWidth = 51;
            dgvCommentViewer.RowTemplate.Height = 29;
            dgvCommentViewer.Size = new Size(390, 219);
            dgvCommentViewer.TabIndex = 2;
            // 
            // lbCommentViewer
            // 
            lbCommentViewer.AutoSize = true;
            lbCommentViewer.Dock = DockStyle.Top;
            lbCommentViewer.Location = new Point(0, 0);
            lbCommentViewer.Name = "lbCommentViewer";
            lbCommentViewer.Size = new Size(59, 20);
            lbCommentViewer.TabIndex = 1;
            lbCommentViewer.Text = "・コメント";
            // 
            // dgvGift
            // 
            dgvGift.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvGift.Dock = DockStyle.Fill;
            dgvGift.Location = new Point(0, 20);
            dgvGift.Name = "dgvGift";
            dgvGift.ReadOnly = true;
            dgvGift.RowHeadersWidth = 51;
            dgvGift.RowTemplate.Height = 29;
            dgvGift.Size = new Size(405, 219);
            dgvGift.TabIndex = 3;
            // 
            // lbGift
            // 
            lbGift.AutoSize = true;
            lbGift.Dock = DockStyle.Top;
            lbGift.Location = new Point(0, 0);
            lbGift.Name = "lbGift";
            lbGift.Size = new Size(93, 20);
            lbGift.TabIndex = 2;
            lbGift.Text = "・スパ茶/ギフト";
            // 
            // TestForm
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 451);
            Controls.Add(mainPanel);
            Controls.Add(ssStatusBar);
            Controls.Add(msMenuBar);
            MainMenuStrip = msMenuBar;
            Name = "TestForm";
            Text = "HCV:テストフォーム";
            msMenuBar.ResumeLayout(false);
            msMenuBar.PerformLayout();
            ssStatusBar.ResumeLayout(false);
            ssStatusBar.PerformLayout();
            mainPanel.ResumeLayout(false);
            mainSplitContainer.Panel1.ResumeLayout(false);
            mainSplitContainer.Panel1.PerformLayout();
            mainSplitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)mainSplitContainer).EndInit();
            mainSplitContainer.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvStreamManager).EndInit();
            subPanel.ResumeLayout(false);
            bottomSplitContainer.Panel1.ResumeLayout(false);
            bottomSplitContainer.Panel1.PerformLayout();
            bottomSplitContainer.Panel2.ResumeLayout(false);
            bottomSplitContainer.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)bottomSplitContainer).EndInit();
            bottomSplitContainer.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvCommentViewer).EndInit();
            ((System.ComponentModel.ISupportInitialize)dgvGift).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private MenuStrip msMenuBar;
        private ToolStripMenuItem tsmiFile;
        private ToolStripMenuItem tsmiClose;
        private StatusStrip ssStatusBar;
        private ToolStripStatusLabel tsslStatus;
        private System.Windows.Forms.Timer commentWatchTimer;
        private Panel mainPanel;
        private SplitContainer mainSplitContainer;
        private DataGridView dgvStreamManager;
        private Label lbStreamManager;
        private Panel subPanel;
        private SplitContainer bottomSplitContainer;
        private DataGridView dgvCommentViewer;
        private Label lbCommentViewer;
        private DataGridView dgvGift;
        private Label lbGift;
    }
}