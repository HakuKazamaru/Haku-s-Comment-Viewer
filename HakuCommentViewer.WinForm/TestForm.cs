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
        /// NLogロガー
        /// </summary>
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        /// <summary>
        /// コメント取得スレッド管理オブジェクト
        /// </summary>
        private Dictionary<int, Task> tasks = new Dictionary<int, Task>();

        /// <summary>
        /// キャンセルトークンソース
        /// </summary>
        private Dictionary<int, CancellationTokenSource> cancellationTokenSourceList = new Dictionary<int, CancellationTokenSource>();

        /// <summary>
        /// コメント取得オブジェクト
        /// </summary>
        private Dictionary<int, Plugin.Interface.IComment> commentFuncList = new Dictionary<int, Plugin.Interface.IComment>();

        /// <summary>
        /// コメント情報受信用
        /// </summary>
        GetNewCommentArgs tmpCommentInfo = new GetNewCommentArgs();

        /// <summary>
        /// コメント情報一覧管理オブジェクト非同期排他フラグ
        /// </summary>
        private bool IsModefyingCommentInfoList = false;

        /// <summary>
        /// コンストラクター
        /// </summary>
        public TestForm()
        {
            logger.Trace("==============================  Start   ==============================");

            InitializeComponent();
            InitializeDgvStreamManager();
            InitializeDgvCommentViewer();
            InitializeDgvGiftViewer();

            // WEBサーバーモジュール起動確認
            if (!WebServer.CheckRunningProcess())
            {
                // アプリケーション配置先取得
                string exePath = Path.Combine(WebServer.ServerWorkPath, WebServer.ServerExeName);
                string msg = "WEBサーバーモジュールの起動に失敗しました。\r\n";
                msg += "起動パス:" + exePath;
                MessageBox.Show(msg, "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                throw new Exception(msg);
            }

            this.tsslStatus.Text = "初期化が完了しました";

            logger.Trace("==============================   End    ==============================");
        }

        /// <summary>
        /// ストリーム管理画面初期化
        /// </summary>
        private void InitializeDgvStreamManager()
        {
            DataGridViewColumn dataGridViewColumn;
            logger.Debug("==============================  Start   ==============================");

            // 列の設定
            // 配信管理番号
            dataGridViewColumn = new DataGridViewTextBoxColumn();
            dataGridViewColumn.Name = "StreamNo";
            dataGridViewColumn.HeaderText = "#";
            dataGridViewColumn.ValueType = typeof(int);
            dataGridViewColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCellsExceptHeader;
            dataGridViewColumn.ReadOnly = true;
            this.dgvStreamManager.Columns.Add(dataGridViewColumn);

            // ストリーム名
            dataGridViewColumn = new DataGridViewTextBoxColumn();
            dataGridViewColumn.Name = "StreamName";
            dataGridViewColumn.HeaderText = "ストリーム名";
            dataGridViewColumn.ValueType = typeof(string);
            dataGridViewColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            dataGridViewColumn.Visible = true;
            dataGridViewColumn.ReadOnly = false;
            this.dgvStreamManager.Columns.Add(dataGridViewColumn);

            // ストリームURL
            dataGridViewColumn = new DataGridViewTextBoxColumn();
            dataGridViewColumn.Name = "StreamURL";
            dataGridViewColumn.HeaderText = "URL";
            dataGridViewColumn.ValueType = typeof(string);
            dataGridViewColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            dataGridViewColumn.ReadOnly = false;
            this.dgvStreamManager.Columns.Add(dataGridViewColumn);

            // 接続済みフラグ
            dataGridViewColumn = new DataGridViewCheckBoxColumn();
            dataGridViewColumn.Name = "IsConnected";
            dataGridViewColumn.HeaderText = "接続";
            dataGridViewColumn.ValueType = typeof(bool);
            dataGridViewColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader;
            dataGridViewColumn.ReadOnly = true;
            this.dgvStreamManager.Columns.Add(dataGridViewColumn);

            // コメジェネ使用フラグ
            dataGridViewColumn = new DataGridViewCheckBoxColumn();
            dataGridViewColumn.Name = "UseCommentGenerator";
            dataGridViewColumn.HeaderText = "ｺﾒｼﾞｪﾈ";
            dataGridViewColumn.ValueType = typeof(bool);
            dataGridViewColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader;
            dataGridViewColumn.ReadOnly = false;
            this.dgvStreamManager.Columns.Add(dataGridViewColumn);

            // コメジェネ使用フラグ
            dataGridViewColumn = new DataGridViewCheckBoxColumn();
            dataGridViewColumn.Name = "UseNarrator";
            dataGridViewColumn.HeaderText = "読上げ";
            dataGridViewColumn.ValueType = typeof(bool);
            dataGridViewColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader;
            dataGridViewColumn.ReadOnly = false;
            this.dgvStreamManager.Columns.Add(dataGridViewColumn);

            // 配信開始経過時間
            dataGridViewColumn = new DataGridViewTextBoxColumn();
            dataGridViewColumn.Name = "ElapsedTime";
            dataGridViewColumn.HeaderText = "経過時間";
            dataGridViewColumn.ValueType = typeof(DateTime);
            dataGridViewColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            dataGridViewColumn.ReadOnly = true;
            this.dgvStreamManager.Columns.Add(dataGridViewColumn);

            // 視聴者数
            dataGridViewColumn = new DataGridViewTextBoxColumn();
            dataGridViewColumn.Name = "ViewerCount";
            dataGridViewColumn.HeaderText = "視聴者数";
            dataGridViewColumn.ValueType = typeof(int);
            dataGridViewColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader;
            dataGridViewColumn.ReadOnly = true;
            this.dgvStreamManager.Columns.Add(dataGridViewColumn);

            // コメント数
            dataGridViewColumn = new DataGridViewTextBoxColumn();
            dataGridViewColumn.Name = "CommentCount";
            dataGridViewColumn.HeaderText = "ｺﾒﾝﾄ数";
            dataGridViewColumn.ValueType = typeof(int);
            dataGridViewColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader;
            dataGridViewColumn.ReadOnly = true;
            this.dgvStreamManager.Columns.Add(dataGridViewColumn);

            // 備考
            dataGridViewColumn = new DataGridViewTextBoxColumn();
            dataGridViewColumn.Name = "Note";
            dataGridViewColumn.HeaderText = "備考";
            dataGridViewColumn.ValueType = typeof(string);
            dataGridViewColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            dataGridViewColumn.Visible = true;
            dataGridViewColumn.ReadOnly = false;
            this.dgvStreamManager.Columns.Add(dataGridViewColumn);

            // アクションボタン
            dataGridViewColumn = new Controls.DataGridViewDisableButtonColumn();
            dataGridViewColumn.Name = "ActionButton";
            dataGridViewColumn.HeaderText = "";
            dataGridViewColumn.ValueType = typeof(string);
            dataGridViewColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            dataGridViewColumn.ReadOnly = false;
            this.dgvStreamManager.Columns.Add(dataGridViewColumn);

            this.dgvStreamManager.Rows[0].Cells[0].Value = 1;
            this.dgvStreamManager.Rows[0].Cells[1].Value = 1;
            this.dgvStreamManager.Rows[0].Cells[10].Value = "接続";

            this.dgvStreamManager.Columns[1].Visible = false;
            this.dgvStreamManager.Columns[9].Visible = false;

            logger.Debug("==============================   End    ==============================");
        }

        /// <summary>
        /// コメント一覧初期化
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

            // 列の設定
            // 配信管理番号
            dataGridViewColumn = new DataGridViewTextBoxColumn();
            dataGridViewColumn.Name = "StreamNo";
            dataGridViewColumn.HeaderText = "#";
            dataGridViewColumn.ValueType = typeof(int);
            dataGridViewColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCellsExceptHeader;
            dataGridViewColumn.Visible = true;
            dataGridViewColumn.ReadOnly = true;
            this.dgvCommentViewer.Columns.Add(dataGridViewColumn);

            // 列の設定
            // 配信管理番号
            dataGridViewColumn = new DataGridViewTextBoxColumn();
            dataGridViewColumn.Name = "StreamSiteId";
            dataGridViewColumn.HeaderText = "配信サイトID";
            dataGridViewColumn.ValueType = typeof(string);
            dataGridViewColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCellsExceptHeader;
            dataGridViewColumn.Visible = false;
            dataGridViewColumn.ReadOnly = true;
            this.dgvCommentViewer.Columns.Add(dataGridViewColumn);

            // ユーザーID
            dataGridViewColumn = new DataGridViewTextBoxColumn();
            dataGridViewColumn.Name = "UserId";
            dataGridViewColumn.HeaderText = "ユーザーID";
            dataGridViewColumn.ValueType = typeof(string);
            dataGridViewColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            dataGridViewColumn.Visible = false;
            dataGridViewColumn.ReadOnly = true;
            this.dgvCommentViewer.Columns.Add(dataGridViewColumn);

            // ユーザー名
            dataGridViewColumn = new DataGridViewTextBoxColumn();
            dataGridViewColumn.Name = "UserName";
            dataGridViewColumn.HeaderText = "ユーザー名";
            dataGridViewColumn.ValueType = typeof(string);
            dataGridViewColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            dataGridViewColumn.Visible = true;
            dataGridViewColumn.ReadOnly = true;
            this.dgvCommentViewer.Columns.Add(dataGridViewColumn);

            // コメント
            dataGridViewColumn = new DataGridViewTextBoxColumn();
            dataGridViewColumn.Name = "CommentText";
            dataGridViewColumn.HeaderText = "コメント";
            dataGridViewColumn.ValueType = typeof(string);
            dataGridViewColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            dataGridViewColumn.Visible = true;
            dataGridViewColumn.ReadOnly = true;
            this.dgvCommentViewer.Columns.Add(dataGridViewColumn);

            // タイムスタンプ
            dataGridViewColumn = new DataGridViewTextBoxColumn();
            dataGridViewColumn.Name = "TimeStampUSec";
            dataGridViewColumn.HeaderText = "タイムスタンプ(UnixTime:秒)";
            dataGridViewColumn.ValueType = typeof(string);
            dataGridViewColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            dataGridViewColumn.Visible = false;
            dataGridViewColumn.ReadOnly = true;
            this.dgvCommentViewer.Columns.Add(dataGridViewColumn);

            logger.Debug("==============================   End    ==============================");
        }

        /// <summary>
        /// ギフト一覧初期化
        /// </summary>
        private void InitializeDgvGiftViewer()
        {
            DataGridViewColumn dataGridViewColumn;
            logger.Debug("==============================  Start   ==============================");

            // 列の設定
            // 配信管理番号
            dataGridViewColumn = new DataGridViewTextBoxColumn();
            dataGridViewColumn.Name = "StreamNo";
            dataGridViewColumn.HeaderText = "#";
            dataGridViewColumn.ValueType = typeof(int);
            dataGridViewColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCellsExceptHeader;
            dataGridViewColumn.ReadOnly = true;
            this.dgvGift.Columns.Add(dataGridViewColumn);

            // ユーザー名
            dataGridViewColumn = new DataGridViewTextBoxColumn();
            dataGridViewColumn.Name = "UserName";
            dataGridViewColumn.HeaderText = "ユーザー名";
            dataGridViewColumn.ValueType = typeof(string);
            dataGridViewColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            dataGridViewColumn.ReadOnly = true;
            this.dgvGift.Columns.Add(dataGridViewColumn);

            // 金額/ポイント
            dataGridViewColumn = new DataGridViewTextBoxColumn();
            dataGridViewColumn.Name = "Valuet";
            dataGridViewColumn.HeaderText = "金額/ポイント";
            dataGridViewColumn.ValueType = typeof(int);
            dataGridViewColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            dataGridViewColumn.ReadOnly = true;
            this.dgvGift.Columns.Add(dataGridViewColumn);

            // コメント
            dataGridViewColumn = new DataGridViewTextBoxColumn();
            dataGridViewColumn.Name = "CommentText";
            dataGridViewColumn.HeaderText = "コメント";
            dataGridViewColumn.ValueType = typeof(int);
            dataGridViewColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            dataGridViewColumn.ReadOnly = true;
            this.dgvGift.Columns.Add(dataGridViewColumn);

            logger.Debug("==============================   End    ==============================");
        }

        /// <summary>
        /// ファイル>閉じる
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tsmiClose_Click(object sender, EventArgs e)
        {
            logger.Trace("==============================   Call   ==============================");
            this.Close();
        }

        /// <summary>
        /// 配信管理情報一覧行追加時
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
                this.dgvStreamManager.Rows[e.RowIndex].Cells[10].Value = "接続";
            }
        }

        /// <summary>
        /// 配信管理情報一覧>セルクリック時
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
                    // ストリーム名テキストボックスクリック
                    {
                        this.dgvStreamManager.BeginEdit(true);
                        break;
                    }
                case 2:
                    // ストリームURLテキストボックスクリック
                    {
                        this.dgvStreamManager.BeginEdit(true);
                        break;
                    }
                case 9:
                    // 備考テキストボックスクリック
                    {
                        this.dgvStreamManager.BeginEdit(true);
                        break;
                    }
                case 10:
                    // アクションボタンクリック
                    {
                        if (this.dgvStreamManager.Rows[e.RowIndex].Cells[10].Value == "接続")
                        {
                            if (this.CheckStreamNameAndUrl(e.RowIndex))
                            {
                                ((DataGridViewDisableButtonCell)this.dgvStreamManager.Rows[e.RowIndex].Cells[10]).Enabled = false;
                                this.cancellationTokenSourceList.Add(e.RowIndex, new());
                                this.tsslStatus.Text = "動画配信サイトに接続を開始しました　No:" + (e.RowIndex + 1);
                                this.tasks.Add(e.RowIndex, Task.Run(() => this.ConnectSite(e.RowIndex, this.cancellationTokenSourceList[e.RowIndex].Token)));
                            }
                        }
                        else if (this.dgvStreamManager.Rows[e.RowIndex].Cells[10].Value == "切断")
                        {
                            ((DataGridViewDisableButtonCell)this.dgvStreamManager.Rows[e.RowIndex].Cells[10]).Enabled = false;
                            this.cancellationTokenSourceList[e.RowIndex].Cancel();
                            this.tsslStatus.Text = "動画配信サイトとの接続解除を開始しました　No:" + (e.RowIndex + 1);

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
                                logger.Warn(ex, "タスクが存在していません。エラーメッセージ:{0}", ex.Message);
                            }

                            this.cancellationTokenSourceList.Remove(e.RowIndex);
                            this.tasks.Remove(e.RowIndex);
                            this.tsslStatus.Text = "動画配信サイトとの接続解除しました　No:" + (e.RowIndex + 1);
                            this.dgvStreamManager.Rows[e.RowIndex].Cells[3].Value = false;
                            this.dgvStreamManager.Rows[e.RowIndex].Cells[10].Value = "接続";
                            ((DataGridViewDisableButtonCell)this.dgvStreamManager.Rows[e.RowIndex].Cells[10]).Enabled = true;
                        }
                        break;
                    }
            }
            logger.Trace("==============================   End    ==============================");
        }

        /// <summary>
        /// 受信コメントUI反映
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void commentWatchTimer_Tick(object sender, EventArgs e)
        {
            logger.Trace("==============================  Start   ==============================");
            bool skipFlag = false;
            int row = (this.dgvCommentViewer.Rows.Count - 2) < 1 ?
                this.dgvCommentViewer.Rows.Count - 1 : this.dgvCommentViewer.Rows.Count - 2;
            string msg = "コメントをコメント情報に追加しました　配信No:{0} ユーザー:{1}　コメント:{2}";
            string tmpString = this.dgvCommentViewer.Rows[row].Cells[4].Value is not null ?
                    this.dgvCommentViewer.Rows[row].Cells[4].Value.ToString() : "";

            // タスク破棄処理
            foreach (var task in this.tasks.Select((Value, Index) => (Value, Index)))
            {
                logger.Trace("[{0}-{1}]タスク情報。リザルト:{2}", task.Index, task.Value.Key, task.Value.Value.Status);
                if (task.Value.Value.IsCompleted)
                {
                    try
                    {
                        this.tasks[task.Value.Key].Dispose();
                        this.tasks.Remove(task.Value.Key);
                        logger.Debug("[{0}-{1}]タスクを破棄しました。リザルト:{2}", task.Index, task.Value.Key, task.Value.Value.Status);
                    }
                    catch (Exception ex)
                    {
                        logger.Warn(ex, "[{0}-{1}]タスク破棄に失敗しました。エラーメッセージ:{2}", task.Index, task.Value.Key, ex.Message);
                    }
                }
            }

            if (!this.IsModefyingCommentInfoList) { return; }

            // コメント重複チェック
            if (!string.IsNullOrWhiteSpace(tmpString))
            {
                //同じコメントが取得済みの場合スキップ
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

            // 画面に反映
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
                        logger.Trace(ex, "行削除に失敗しました。エラーメッセージ:{0}", ex.Message);
                    }
                }
                this.dgvCommentViewer.CommitEdit(DataGridViewDataErrorContexts.Commit);

                this.dgvCommentViewer.CurrentCell = this.dgvCommentViewer[1, 0];

                msg = "コメントを反映しました　配信No:{0} ユーザー:{1}　コメント:{2}";
                logger.Trace(msg, tmpCommentInfo.StreamNo, tmpCommentInfo.UserName, tmpCommentInfo.CommentText);
                this.tsslStatus.Text = string.Format(msg, tmpCommentInfo.StreamNo, tmpCommentInfo.UserName, tmpCommentInfo.CommentText);
            }

            this.IsModefyingCommentInfoList = false;
            logger.Trace("==============================   End    ==============================");
        }

        /// <summary>
        /// コメント受信時の処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnGetComment(object sender, GetNewCommentArgs args)
        {
            string msg = "コメントを受信しました　配信No:{0} ユーザー:{1}　コメント:{2}";
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
        /// ストリーム名、URL入力チェック
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

                if (string.IsNullOrWhiteSpace(streamName)) msg += "ストリーム名";
                if (string.IsNullOrWhiteSpace(streamUrl)) msg += (string.IsNullOrWhiteSpace(msg) ? "" : " と ") + "URL";

                msg = string.Format("{0} を入力してください。", msg);

                MessageBox.Show(msg, "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                    msg = "URLに入力されたアドレスがURL警視ではありません。";
                    MessageBox.Show(msg, "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    logger.Error(msg);
                }
            }

            logger.Trace("==============================   End    ==============================");
            return returnVal;
        }

        /// <summary>
        /// 配信サイト接続処理
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
                this.tsslStatus.Text = "YouTube Liveへ接続します　No:" + (row + 1);

                await Task.Run(async () =>
                {
                    commentFuncList.Add(row, new YouTube.Comment(row, url));
                    commentFuncList[row].GetCommentHandler += OnGetComment;
                    result = await commentFuncList[row].StartAsync(cancellationToken);
                });

                if (result)
                {
                    this.tsslStatus.Text = "YouTube Liveへの接続が完了しました　No:" + (row + 1);
                    this.dgvStreamManager.Rows[row].Cells[7].Value = commentFuncList[row].StreamInfo.ViewerCount.ToString();
                    this.dgvStreamManager.Rows[row].Cells[8].Value = commentFuncList[row].StreamInfo.CommentCount.ToString();
                    this.dgvStreamManager.Rows[row].Cells[10].Value = "切断";
                    this.dgvStreamManager.Rows[row].Cells[3].Value = true;
                }
                else
                {
                    this.tsslStatus.Text = "YouTube Liveへの接続が失敗しました　No:" + (row + 1);
                    this.dgvStreamManager.Rows[row].Cells[3].Value = false;
                    commentFuncList[row].Dispose();
                    commentFuncList.Remove(row);
                }
            }
            else if (url.IndexOf("nicovideo.jp") > -1)
            {
                this.tsslStatus.Text = "ニコニコ生放送へ接続します　No:" + (row + 1);

                await Task.Run(async () =>
                {
                    commentFuncList.Add(row, new NicoNico.Comment(row, url));
                    commentFuncList[row].GetCommentHandler += OnGetComment;
                    result = await commentFuncList[row].StartAsync(cancellationToken);
                });

                if (result)
                {
                    this.tsslStatus.Text = "ニコニコ生放送への接続が完了しました　No:" + (row + 1);
                    this.dgvStreamManager.Rows[row].Cells[7].Value = commentFuncList[row].StreamInfo.ViewerCount.ToString();
                    this.dgvStreamManager.Rows[row].Cells[8].Value = commentFuncList[row].StreamInfo.CommentCount.ToString();
                    this.dgvStreamManager.Rows[row].Cells[10].Value = "切断";
                    this.dgvStreamManager.Rows[row].Cells[3].Value = true;
                }
                else
                {
                    this.tsslStatus.Text = "ニコニコ生放送への接続が失敗しました　No:" + (row + 1);
                    this.dgvStreamManager.Rows[row].Cells[3].Value = false;
                    commentFuncList[row].Dispose();
                    commentFuncList.Remove(row);
                }
            }
            else
            {
                this.tsslStatus.Text = "非対応のサイトです　No:" + (row + 1);
                this.dgvStreamManager.Rows[row].Cells[3].Value = false;
            }

            ((DataGridViewDisableButtonCell)this.dgvStreamManager.Rows[row].Cells[10]).Enabled = true;

            return result;
        }

    }
}