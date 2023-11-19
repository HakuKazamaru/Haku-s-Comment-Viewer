using System.IO;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows.Forms;

using NLog;

using HakuCommentViewer;
using HakuCommentViewer.Common;
using HakuCommentViewer.Common.Models;
using Plugin = HakuCommentViewer.Plugin;
using NicoNico = HakuCommentViewer.Plugin.Comment.NicoNico;
using YouTube = HakuCommentViewer.Plugin.Comment.YouTube;
using HakuCommentViewer.Plugin.Models;
using AngleSharp.Dom;
using HakuCommentViewer.WinForm.Controls;
using NLog.Fluent;

namespace HakuCommentViewer.WinForm
{
    public partial class TestForm : Form
    {
        /// <summary>
        /// NLog���K�[
        /// </summary>
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        /// <summary>
        /// �R�����g�擾�X���b�h�Ǘ��I�u�W�F�N�g
        /// </summary>
        private Dictionary<int, Task> tasks = new Dictionary<int, Task>();

        /// <summary>
        /// �L�����Z���g�[�N���\�[�X
        /// </summary>
        private Dictionary<int, CancellationTokenSource> cancellationTokenSourceList = new Dictionary<int, CancellationTokenSource>();

        /// <summary>
        /// �R�����g�擾�I�u�W�F�N�g
        /// </summary>
        private Dictionary<int, Plugin.Interface.IComment> commentFuncList = new Dictionary<int, Plugin.Interface.IComment>();

        /// <summary>
        /// �R�����g����M�p
        /// </summary>
        GetNewCommentArgs tmpCommentInfo = new GetNewCommentArgs();

        /// <summary>
        /// �R�����g���ꗗ�Ǘ��I�u�W�F�N�g�񓯊��r���t���O
        /// </summary>
        private bool IsModefyingCommentInfoList = false;

        /// <summary>
        /// �R���X�g���N�^�[
        /// </summary>
        public TestForm()
        {
            logger.Trace("==============================  Start   ==============================");

            InitializeComponent();
            InitializeDgvStreamManager();
            InitializeDgvCommentViewer();
            InitializeDgvGiftViewer();

            // WEB�T�[�o�[���W���[���N���m�F
            if (!WebServer.CheckRunningProcess())
            {
                // �A�v���P�[�V�����z�u��擾
                string exePath = Path.Combine(WebServer.ServerWorkPath, WebServer.ServerExeName);
                string msg = "WEB�T�[�o�[���W���[���̋N���Ɏ��s���܂����B\r\n";
                msg += "�N���p�X:" + exePath;
                MessageBox.Show(msg, "�G���[", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                throw new Exception(msg);
            }

            this.tsslStatus.Text = "���������������܂���";

            logger.Trace("==============================   End    ==============================");
        }

        /// <summary>
        /// �X�g���[���Ǘ���ʏ�����
        /// </summary>
        private void InitializeDgvStreamManager()
        {
            DataGridViewColumn dataGridViewColumn;
            logger.Debug("==============================  Start   ==============================");

            // ��̐ݒ�
            // �z�M�Ǘ��ԍ�
            dataGridViewColumn = new DataGridViewTextBoxColumn();
            dataGridViewColumn.Name = "StreamNo";
            dataGridViewColumn.HeaderText = "#";
            dataGridViewColumn.ValueType = typeof(int);
            dataGridViewColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCellsExceptHeader;
            dataGridViewColumn.ReadOnly = true;
            this.dgvStreamManager.Columns.Add(dataGridViewColumn);

            // �X�g���[����
            dataGridViewColumn = new DataGridViewTextBoxColumn();
            dataGridViewColumn.Name = "StreamName";
            dataGridViewColumn.HeaderText = "�X�g���[����";
            dataGridViewColumn.ValueType = typeof(string);
            dataGridViewColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            dataGridViewColumn.Visible = true;
            dataGridViewColumn.ReadOnly = false;
            this.dgvStreamManager.Columns.Add(dataGridViewColumn);

            // �X�g���[��URL
            dataGridViewColumn = new DataGridViewTextBoxColumn();
            dataGridViewColumn.Name = "StreamURL";
            dataGridViewColumn.HeaderText = "URL";
            dataGridViewColumn.ValueType = typeof(string);
            dataGridViewColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            dataGridViewColumn.ReadOnly = false;
            this.dgvStreamManager.Columns.Add(dataGridViewColumn);

            // �ڑ��ς݃t���O
            dataGridViewColumn = new DataGridViewCheckBoxColumn();
            dataGridViewColumn.Name = "IsConnected";
            dataGridViewColumn.HeaderText = "�ڑ�";
            dataGridViewColumn.ValueType = typeof(bool);
            dataGridViewColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader;
            dataGridViewColumn.ReadOnly = true;
            this.dgvStreamManager.Columns.Add(dataGridViewColumn);

            // �R���W�F�l�g�p�t���O
            dataGridViewColumn = new DataGridViewCheckBoxColumn();
            dataGridViewColumn.Name = "UseCommentGenerator";
            dataGridViewColumn.HeaderText = "�Ҽު�";
            dataGridViewColumn.ValueType = typeof(bool);
            dataGridViewColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader;
            dataGridViewColumn.ReadOnly = false;
            this.dgvStreamManager.Columns.Add(dataGridViewColumn);

            // �R���W�F�l�g�p�t���O
            dataGridViewColumn = new DataGridViewCheckBoxColumn();
            dataGridViewColumn.Name = "UseNarrator";
            dataGridViewColumn.HeaderText = "�Ǐグ";
            dataGridViewColumn.ValueType = typeof(bool);
            dataGridViewColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader;
            dataGridViewColumn.ReadOnly = false;
            this.dgvStreamManager.Columns.Add(dataGridViewColumn);

            // �z�M�J�n�o�ߎ���
            dataGridViewColumn = new DataGridViewTextBoxColumn();
            dataGridViewColumn.Name = "ElapsedTime";
            dataGridViewColumn.HeaderText = "�o�ߎ���";
            dataGridViewColumn.ValueType = typeof(DateTime);
            dataGridViewColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            dataGridViewColumn.ReadOnly = true;
            this.dgvStreamManager.Columns.Add(dataGridViewColumn);

            // �����Ґ�
            dataGridViewColumn = new DataGridViewTextBoxColumn();
            dataGridViewColumn.Name = "ViewerCount";
            dataGridViewColumn.HeaderText = "�����Ґ�";
            dataGridViewColumn.ValueType = typeof(int);
            dataGridViewColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader;
            dataGridViewColumn.ReadOnly = true;
            this.dgvStreamManager.Columns.Add(dataGridViewColumn);

            // �R�����g��
            dataGridViewColumn = new DataGridViewTextBoxColumn();
            dataGridViewColumn.Name = "CommentCount";
            dataGridViewColumn.HeaderText = "���Đ�";
            dataGridViewColumn.ValueType = typeof(int);
            dataGridViewColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader;
            dataGridViewColumn.ReadOnly = true;
            this.dgvStreamManager.Columns.Add(dataGridViewColumn);

            // ���l
            dataGridViewColumn = new DataGridViewTextBoxColumn();
            dataGridViewColumn.Name = "Note";
            dataGridViewColumn.HeaderText = "���l";
            dataGridViewColumn.ValueType = typeof(string);
            dataGridViewColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            dataGridViewColumn.Visible = true;
            dataGridViewColumn.ReadOnly = false;
            this.dgvStreamManager.Columns.Add(dataGridViewColumn);

            // �A�N�V�����{�^��
            dataGridViewColumn = new Controls.DataGridViewDisableButtonColumn();
            dataGridViewColumn.Name = "ActionButton";
            dataGridViewColumn.HeaderText = "";
            dataGridViewColumn.ValueType = typeof(string);
            dataGridViewColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            dataGridViewColumn.ReadOnly = false;
            this.dgvStreamManager.Columns.Add(dataGridViewColumn);

            this.dgvStreamManager.Rows[0].Cells[0].Value = 1;
            this.dgvStreamManager.Rows[0].Cells[1].Value = 1;
            this.dgvStreamManager.Rows[0].Cells[10].Value = "�ڑ�";

            this.dgvStreamManager.Columns[1].Visible = false;
            this.dgvStreamManager.Columns[9].Visible = false;

            logger.Debug("==============================   End    ==============================");
        }

        /// <summary>
        /// �R�����g�ꗗ������
        /// </summary>
        private void InitializeDgvCommentViewer()
        {
            DataGridViewColumn dataGridViewColumn;
            logger.Debug("==============================  Start   ==============================");

            // ID
            dataGridViewColumn = new DataGridViewTextBoxColumn();
            dataGridViewColumn.Name = "Id";
            dataGridViewColumn.HeaderText = "ID";
            dataGridViewColumn.ValueType = typeof(string);
            dataGridViewColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            dataGridViewColumn.Visible = false;
            dataGridViewColumn.ReadOnly = true;
            this.dgvCommentViewer.Columns.Add(dataGridViewColumn);

            // ��̐ݒ�
            // �z�M�Ǘ��ԍ�
            dataGridViewColumn = new DataGridViewTextBoxColumn();
            dataGridViewColumn.Name = "StreamNo";
            dataGridViewColumn.HeaderText = "#";
            dataGridViewColumn.ValueType = typeof(int);
            dataGridViewColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCellsExceptHeader;
            dataGridViewColumn.Visible = true;
            dataGridViewColumn.ReadOnly = true;
            this.dgvCommentViewer.Columns.Add(dataGridViewColumn);

            // ��̐ݒ�
            // �z�M�Ǘ��ԍ�
            dataGridViewColumn = new DataGridViewTextBoxColumn();
            dataGridViewColumn.Name = "StreamSiteId";
            dataGridViewColumn.HeaderText = "�z�M�T�C�gID";
            dataGridViewColumn.ValueType = typeof(string);
            dataGridViewColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCellsExceptHeader;
            dataGridViewColumn.Visible = false;
            dataGridViewColumn.ReadOnly = true;
            this.dgvCommentViewer.Columns.Add(dataGridViewColumn);

            // ���[�U�[ID
            dataGridViewColumn = new DataGridViewTextBoxColumn();
            dataGridViewColumn.Name = "UserId";
            dataGridViewColumn.HeaderText = "���[�U�[ID";
            dataGridViewColumn.ValueType = typeof(string);
            dataGridViewColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            dataGridViewColumn.Visible = false;
            dataGridViewColumn.ReadOnly = true;
            this.dgvCommentViewer.Columns.Add(dataGridViewColumn);

            // ���[�U�[��
            dataGridViewColumn = new DataGridViewTextBoxColumn();
            dataGridViewColumn.Name = "UserName";
            dataGridViewColumn.HeaderText = "���[�U�[��";
            dataGridViewColumn.ValueType = typeof(string);
            dataGridViewColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            dataGridViewColumn.Visible = true;
            dataGridViewColumn.ReadOnly = true;
            this.dgvCommentViewer.Columns.Add(dataGridViewColumn);

            // �R�����g
            dataGridViewColumn = new DataGridViewTextBoxColumn();
            dataGridViewColumn.Name = "CommentText";
            dataGridViewColumn.HeaderText = "�R�����g";
            dataGridViewColumn.ValueType = typeof(string);
            dataGridViewColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            dataGridViewColumn.Visible = true;
            dataGridViewColumn.ReadOnly = true;
            this.dgvCommentViewer.Columns.Add(dataGridViewColumn);

            // �^�C���X�^���v
            dataGridViewColumn = new DataGridViewTextBoxColumn();
            dataGridViewColumn.Name = "TimeStampUSec";
            dataGridViewColumn.HeaderText = "�^�C���X�^���v(UnixTime:�b)";
            dataGridViewColumn.ValueType = typeof(string);
            dataGridViewColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            dataGridViewColumn.Visible = false;
            dataGridViewColumn.ReadOnly = true;
            this.dgvCommentViewer.Columns.Add(dataGridViewColumn);

            logger.Debug("==============================   End    ==============================");
        }

        /// <summary>
        /// �M�t�g�ꗗ������
        /// </summary>
        private void InitializeDgvGiftViewer()
        {
            DataGridViewColumn dataGridViewColumn;
            logger.Debug("==============================  Start   ==============================");

            // ��̐ݒ�
            // �z�M�Ǘ��ԍ�
            dataGridViewColumn = new DataGridViewTextBoxColumn();
            dataGridViewColumn.Name = "StreamNo";
            dataGridViewColumn.HeaderText = "#";
            dataGridViewColumn.ValueType = typeof(int);
            dataGridViewColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCellsExceptHeader;
            dataGridViewColumn.ReadOnly = true;
            this.dgvGift.Columns.Add(dataGridViewColumn);

            // ���[�U�[��
            dataGridViewColumn = new DataGridViewTextBoxColumn();
            dataGridViewColumn.Name = "UserName";
            dataGridViewColumn.HeaderText = "���[�U�[��";
            dataGridViewColumn.ValueType = typeof(string);
            dataGridViewColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            dataGridViewColumn.ReadOnly = true;
            this.dgvGift.Columns.Add(dataGridViewColumn);

            // ���z/�|�C���g
            dataGridViewColumn = new DataGridViewTextBoxColumn();
            dataGridViewColumn.Name = "Valuet";
            dataGridViewColumn.HeaderText = "���z/�|�C���g";
            dataGridViewColumn.ValueType = typeof(int);
            dataGridViewColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            dataGridViewColumn.ReadOnly = true;
            this.dgvGift.Columns.Add(dataGridViewColumn);

            // �R�����g
            dataGridViewColumn = new DataGridViewTextBoxColumn();
            dataGridViewColumn.Name = "CommentText";
            dataGridViewColumn.HeaderText = "�R�����g";
            dataGridViewColumn.ValueType = typeof(int);
            dataGridViewColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            dataGridViewColumn.ReadOnly = true;
            this.dgvGift.Columns.Add(dataGridViewColumn);

            logger.Debug("==============================   End    ==============================");
        }

        /// <summary>
        /// �t�@�C��>����
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tsmiClose_Click(object sender, EventArgs e)
        {
            logger.Trace("==============================   Call   ==============================");
            this.Close();
        }

        /// <summary>
        /// �z�M�Ǘ����ꗗ�s�ǉ���
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dgvStreamManager_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            logger.Trace("==============================   Call   ==============================");

            if (this.dgvStreamManager.Rows[e.RowIndex].Cells.Count == 11)
            {
                this.dgvStreamManager.Rows[e.RowIndex].Cells[0].Value = e.RowIndex + 1;
                this.dgvStreamManager.Rows[e.RowIndex].Cells[1].Value = e.RowIndex + 1;
                this.dgvStreamManager.Rows[e.RowIndex].Cells[10].Value = "�ڑ�";
            }
        }

        /// <summary>
        /// �z�M�Ǘ����ꗗ>�Z���N���b�N��
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void dgvStreamManager_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            logger.Trace("==============================  Start   ==============================");
            DataGridViewRow dataGridViewRow = this.dgvStreamManager.Rows[e.RowIndex];

            switch (e.ColumnIndex)
            {
                case 1:
                    // �X�g���[�����e�L�X�g�{�b�N�X�N���b�N
                    {
                        this.dgvStreamManager.BeginEdit(true);
                        break;
                    }
                case 2:
                    // �X�g���[��URL�e�L�X�g�{�b�N�X�N���b�N
                    {
                        this.dgvStreamManager.BeginEdit(true);
                        break;
                    }
                case 9:
                    // ���l�e�L�X�g�{�b�N�X�N���b�N
                    {
                        this.dgvStreamManager.BeginEdit(true);
                        break;
                    }
                case 10:
                    // �A�N�V�����{�^���N���b�N
                    {
                        if (this.dgvStreamManager.Rows[e.RowIndex].Cells[10].Value == "�ڑ�")
                        {
                            if (this.CheckStreamNameAndUrl(e.RowIndex))
                            {
                                ((DataGridViewDisableButtonCell)this.dgvStreamManager.Rows[e.RowIndex].Cells[10]).Enabled = false;
                                this.cancellationTokenSourceList.Add(e.RowIndex, new());
                                this.tsslStatus.Text = "����z�M�T�C�g�ɐڑ����J�n���܂����@No:" + (e.RowIndex + 1);
                                this.tasks.Add(e.RowIndex, Task.Run(() => this.ConnectSite(e.RowIndex, this.cancellationTokenSourceList[e.RowIndex].Token)));
                            }
                        }
                        else if (this.dgvStreamManager.Rows[e.RowIndex].Cells[10].Value == "�ؒf")
                        {
                            ((DataGridViewDisableButtonCell)this.dgvStreamManager.Rows[e.RowIndex].Cells[10]).Enabled = false;
                            this.cancellationTokenSourceList[e.RowIndex].Cancel();
                            this.tsslStatus.Text = "����z�M�T�C�g�Ƃ̐ڑ��������J�n���܂����@No:" + (e.RowIndex + 1);

                            try
                            {
                                using (Task task = Task.Run(() => this.tasks[e.RowIndex].Wait()))
                                {
                                    while (!task.IsCompleted)
                                    {
                                        Thread.Sleep(250);
                                        Application.DoEvents();
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                logger.Warn(ex, "�^�X�N�����݂��Ă��܂���B�G���[���b�Z�[�W:{0}", ex.Message);
                            }

                            this.cancellationTokenSourceList.Remove(e.RowIndex);
                            this.tasks.Remove(e.RowIndex);
                            this.tsslStatus.Text = "����z�M�T�C�g�Ƃ̐ڑ��������܂����@No:" + (e.RowIndex + 1);
                            this.dgvStreamManager.Rows[e.RowIndex].Cells[3].Value = false;
                            this.dgvStreamManager.Rows[e.RowIndex].Cells[10].Value = "�ڑ�";
                            ((DataGridViewDisableButtonCell)this.dgvStreamManager.Rows[e.RowIndex].Cells[10]).Enabled = true;
                        }
                        break;
                    }
            }
            logger.Trace("==============================   End    ==============================");
        }

        /// <summary>
        /// ��M�R�����gUI���f
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void commentWatchTimer_Tick(object sender, EventArgs e)
        {
            logger.Trace("==============================  Start   ==============================");
            bool skipFlag = false;
            int row = (this.dgvCommentViewer.Rows.Count - 2) < 1 ?
                this.dgvCommentViewer.Rows.Count - 1 : this.dgvCommentViewer.Rows.Count - 2;
            string msg = "�R�����g���R�����g���ɒǉ����܂����@�z�MNo:{0} ���[�U�[:{1}�@�R�����g:{2}";
            string tmpString = this.dgvCommentViewer.Rows[row].Cells[4].Value is not null ?
                    this.dgvCommentViewer.Rows[row].Cells[4].Value.ToString() : "";

            // �^�X�N�j������
            foreach (var task in this.tasks.Select((Value, Index) => (Value, Index)))
            {
                logger.Trace("[{0}-{1}]�^�X�N���B���U���g:{2}", task.Index, task.Value.Key, task.Value.Value.Status);
                if (task.Value.Value.IsCompleted)
                {
                    try
                    {
                        this.tasks[task.Value.Key].Dispose();
                        this.tasks.Remove(task.Value.Key);
                        logger.Debug("[{0}-{1}]�^�X�N��j�����܂����B���U���g:{2}", task.Index, task.Value.Key, task.Value.Value.Status);
                    }
                    catch (Exception ex)
                    {
                        logger.Warn(ex, "[{0}-{1}]�^�X�N�j���Ɏ��s���܂����B�G���[���b�Z�[�W:{2}", task.Index, task.Value.Key, ex.Message);
                    }
                }
            }

            if (!this.IsModefyingCommentInfoList) { return; }

            // �R�����g�d���`�F�b�N
            if (!string.IsNullOrWhiteSpace(tmpString))
            {
                //�����R�����g���擾�ς݂̏ꍇ�X�L�b�v
                for (int i = 0; i < this.dgvCommentViewer.Rows.Count - 1; i++)
                {
                    var dgvCommentViewerRow = this.dgvCommentViewer.Rows[i];
                    if ((int)dgvCommentViewerRow.Cells[1].Value == (tmpCommentInfo.StreamNo + 1) &&
                        dgvCommentViewerRow.Cells[0].Value.ToString() == tmpCommentInfo.CommentId &&
                        dgvCommentViewerRow.Cells[6].Value.ToString() == tmpCommentInfo.TimeStampUSec)
                    {
                        skipFlag = true;
                        break;
                    }
                }
            }

            // ��ʂɔ��f
            if (!skipFlag)
            {
                DataGridViewRow dataGridViewRow = (DataGridViewRow)this.dgvCommentViewer.Rows[0].Clone();

                dataGridViewRow.Cells[0].Value = tmpCommentInfo.CommentId;
                dataGridViewRow.Cells[1].Value = tmpCommentInfo.StreamNo + 1;
                dataGridViewRow.Cells[2].Value = "";
                dataGridViewRow.Cells[3].Value = tmpCommentInfo.UserId;
                dataGridViewRow.Cells[4].Value = tmpCommentInfo.UserName;
                dataGridViewRow.Cells[5].Value = tmpCommentInfo.CommentText;
                dataGridViewRow.Cells[6].Value = tmpCommentInfo.TimeStampUSec;
                this.dgvCommentViewer.Rows.Insert(0, dataGridViewRow);
                this.dgvCommentViewer.CommitEdit(DataGridViewDataErrorContexts.Commit);

                while (this.dgvCommentViewer.Rows.Count > 1000)
                {
                    try
                    {
                        this.dgvCommentViewer.Rows.RemoveAt(this.dgvCommentViewer.Rows.Count - 1);
                    }
                    catch (Exception ex)
                    {
                        logger.Trace(ex, "�s�폜�Ɏ��s���܂����B�G���[���b�Z�[�W:{0}", ex.Message);
                    }
                }
                this.dgvCommentViewer.CommitEdit(DataGridViewDataErrorContexts.Commit);

                this.dgvCommentViewer.CurrentCell = this.dgvCommentViewer[1, 0];

                msg = "�R�����g�𔽉f���܂����@�z�MNo:{0} ���[�U�[:{1}�@�R�����g:{2}";
                logger.Trace(msg, tmpCommentInfo.StreamNo, tmpCommentInfo.UserName, tmpCommentInfo.CommentText);
                this.tsslStatus.Text = string.Format(msg, tmpCommentInfo.StreamNo, tmpCommentInfo.UserName, tmpCommentInfo.CommentText);
            }

            this.IsModefyingCommentInfoList = false;
            logger.Trace("==============================   End    ==============================");
        }

        /// <summary>
        /// �R�����g��M���̏���
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnGetComment(object sender, GetNewCommentArgs args)
        {
            string msg = "�R�����g����M���܂����@�z�MNo:{0} ���[�U�[:{1}�@�R�����g:{2}";
            logger.Trace("==============================  Start   ==============================");
            logger.Trace("StreamNo:{0}, UserName:{1}, CommentText:{2}",
                args.StreamNo, args.UserName, args.CommentText);

            while (this.IsModefyingCommentInfoList)
            {
                Thread.Sleep(50);
                Application.DoEvents();
            }

            this.IsModefyingCommentInfoList = true;

            tmpCommentInfo.CommentId = args.CommentId;
            tmpCommentInfo.StreamNo = args.StreamNo;
            tmpCommentInfo.UserId = args.UserId;
            tmpCommentInfo.UserName = args.UserName;
            tmpCommentInfo.CommentText = args.CommentText;
            tmpCommentInfo.TimeStampUSec = args.TimeStampUSec;

            logger.Debug(msg, args.StreamNo, args.UserName, args.CommentText);
            logger.Trace("==============================   End    ==============================");
        }

        /// <summary>
        /// �X�g���[�����AURL���̓`�F�b�N
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        private bool CheckStreamNameAndUrl(int row)
        {
            bool returnVal = false;
            string msg = "";
            logger.Trace("==============================  Start   ==============================");

            string? streamName = this.dgvStreamManager.Rows[row].Cells[1].Value is not null ?
                this.dgvStreamManager.Rows[row].Cells[1].Value.ToString() : "",
                streamUrl = this.dgvStreamManager.Rows[row].Cells[2].Value is not null ?
                this.dgvStreamManager.Rows[row].Cells[2].Value.ToString() : "";

            if (string.IsNullOrWhiteSpace(streamName) || string.IsNullOrWhiteSpace(streamUrl))
            {

                if (string.IsNullOrWhiteSpace(streamName)) msg += "�X�g���[����";
                if (string.IsNullOrWhiteSpace(streamUrl)) msg += (string.IsNullOrWhiteSpace(msg) ? "" : " �� ") + "URL";

                msg = string.Format("{0} ����͂��Ă��������B", msg);

                MessageBox.Show(msg, "�G���[", MessageBoxButtons.OK, MessageBoxIcon.Error);
                logger.Error(msg);
            }
            else
            {
                if (Regex.IsMatch(streamUrl, @"^s?https?://[-_.!~*'()a-zA-Z0-9;/?:@&=+$,%#]+$"))
                {
                    returnVal = true;
                }
                else
                {
                    msg = "URL�ɓ��͂��ꂽ�A�h���X��URL�x���ł͂���܂���B";
                    MessageBox.Show(msg, "�G���[", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    logger.Error(msg);
                }
            }

            logger.Trace("==============================   End    ==============================");
            return returnVal;
        }

        /// <summary>
        /// �z�M�T�C�g�ڑ�����
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        private async Task<bool> ConnectSite(int row, CancellationToken cancellationToken)
        {
            bool result = false;
            logger.Debug("==============================   CaLL   ==============================");

            string url = this.dgvStreamManager.Rows[row].Cells[2].Value.ToString();

            if (url.IndexOf("youtube.com") > -1)
            {
                this.tsslStatus.Text = "YouTube Live�֐ڑ����܂��@No:" + (row + 1);

                await Task.Run(async () =>
                {
                    commentFuncList.Add(row, new YouTube.Comment(row, url));
                    commentFuncList[row].GetCommentHandler += OnGetComment;
                    result = await commentFuncList[row].StartAsync(cancellationToken);
                });

                if (result)
                {
                    this.tsslStatus.Text = "YouTube Live�ւ̐ڑ����������܂����@No:" + (row + 1);
                    this.dgvStreamManager.Rows[row].Cells[7].Value = commentFuncList[row].StreamInfo.ViewerCount.ToString();
                    this.dgvStreamManager.Rows[row].Cells[8].Value = commentFuncList[row].StreamInfo.CommentCount.ToString();
                    this.dgvStreamManager.Rows[row].Cells[10].Value = "�ؒf";
                    this.dgvStreamManager.Rows[row].Cells[3].Value = true;
                }
                else
                {
                    this.tsslStatus.Text = "YouTube Live�ւ̐ڑ������s���܂����@No:" + (row + 1);
                    this.dgvStreamManager.Rows[row].Cells[3].Value = false;
                    commentFuncList[row].Dispose();
                    commentFuncList.Remove(row);
                }
            }
            else if (url.IndexOf("nicovideo.jp") > -1)
            {
                this.tsslStatus.Text = "�j�R�j�R�������֐ڑ����܂��@No:" + (row + 1);

                await Task.Run(async () =>
                {
                    commentFuncList.Add(row, new NicoNico.Comment(row, url));
                    commentFuncList[row].GetCommentHandler += OnGetComment;
                    result = await commentFuncList[row].StartAsync(cancellationToken);
                });

                if (result)
                {
                    this.tsslStatus.Text = "�j�R�j�R�������ւ̐ڑ����������܂����@No:" + (row + 1);
                    this.dgvStreamManager.Rows[row].Cells[7].Value = commentFuncList[row].StreamInfo.ViewerCount.ToString();
                    this.dgvStreamManager.Rows[row].Cells[8].Value = commentFuncList[row].StreamInfo.CommentCount.ToString();
                    this.dgvStreamManager.Rows[row].Cells[10].Value = "�ؒf";
                    this.dgvStreamManager.Rows[row].Cells[3].Value = true;
                }
                else
                {
                    this.tsslStatus.Text = "�j�R�j�R�������ւ̐ڑ������s���܂����@No:" + (row + 1);
                    this.dgvStreamManager.Rows[row].Cells[3].Value = false;
                    commentFuncList[row].Dispose();
                    commentFuncList.Remove(row);
                }
            }
            else
            {
                this.tsslStatus.Text = "��Ή��̃T�C�g�ł��@No:" + (row + 1);
                this.dgvStreamManager.Rows[row].Cells[3].Value = false;
            }

            ((DataGridViewDisableButtonCell)this.dgvStreamManager.Rows[row].Cells[10]).Enabled = true;

            return result;
        }

    }
}