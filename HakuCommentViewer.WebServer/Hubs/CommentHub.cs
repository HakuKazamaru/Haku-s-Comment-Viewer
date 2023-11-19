using Microsoft.AspNetCore.SignalR;

using HakuCommentViewer.Common.Models;
using HakuCommentViewer.WebServer.Queues;
using HakuCommentViewer.WebClient.Shared;

using NicoNico = HakuCommentViewer.Plugin.Comment.NicoNico;
using YouTube = HakuCommentViewer.Plugin.Comment.YouTube;

namespace HakuCommentViewer.WebServer.Hubs
{
    public class CommentHub : Hub
    {
        public const string HubUrl = "/commenthub";

        /// <summary>
        /// ロガー
        /// </summary>
        private readonly ILogger<CommentHub> _logger;

        /// <summary>
        /// DBコンテキスト
        /// </summary>
        private readonly Common.HcvDbContext _context;

        /// <summary>
        /// バックグラウンドタスクキュー
        /// </summary>
        private readonly IBackgroundTaskQueue _queue;

        /// <summary>
        /// コンストラクター
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="context"></param>
        public CommentHub(ILogger<CommentHub> logger, Common.HcvDbContext context, IBackgroundTaskQueue queue)
        {
            this._context = context;
            this._logger = logger;
            this._queue = queue;
        }

        /// <summary>
        /// コメント受信処理
        /// </summary>
        /// <param name="timeStampUSec"></param>
        /// <param name="commentId"></param>
        /// <param name="streamNo"></param>
        /// <param name="userId"></param>
        /// <param name="userName"></param>
        /// <param name="commentText"></param>
        /// <returns></returns>
        public async Task ReceiveMessage(string timeStampUSec, string commentId, int streamNo, string userId, string userName, string commentText)
        {
            _logger.LogTrace("==============================   Call   ==============================");
            await Clients.All.SendAsync("ReceiveMessage", timeStampUSec, commentId, streamNo, userId, userName, commentText);
        }

        /// <summary>
        /// ギフト受信処理
        /// </summary>
        /// <param name="timeStampUSec"></param>
        /// <param name="commentId"></param>
        /// <param name="streamNo"></param>
        /// <param name="userId"></param>
        /// <param name="userName"></param>
        /// <param name="giftType"></param>
        /// <param name="giftValue"></param>
        /// <param name="commentText"></param>
        /// <returns></returns>
        public async Task ReceiveGift(string timeStampUSec, string commentId, int streamNo, string userId, string userName, string giftType, int giftValue, string commentText)
        {
            _logger.LogDebug("==============================   Call   ==============================");
            await Clients.All.SendAsync("ReceiveGift", timeStampUSec, commentId, streamNo, userId, userName, giftType, giftValue, commentText);
        }

        /// <summary>
        /// 接続処理結果送信
        /// </summary>
        /// <param name="streamNo"></param>
        /// <param name="result"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task StartResult(string streamNo, bool result, string message)
        {
            _logger.LogDebug("==============================   Call   ==============================");
            await Clients.All.SendAsync("StartResult", streamNo, result, message);
        }

        /// <summary>
        /// コメント受信開始
        /// </summary>
        /// <param name="streamNo"></param>
        /// <param name="streamManagementId"></param>
        /// <param name="streamUrl"></param>
        /// <returns></returns>
        public async Task Start(string streamNo, string streamManagementId, string streamUrl)
        {
            bool result = false;
            int tmpInt = -1;
            _logger.LogDebug("==============================  Start   ==============================");

            try
            {
                int.TryParse(streamNo, out tmpInt);

                // DBのキューを取得
                var streamManagementInfo = _context.StreamManagementInfos.SingleOrDefault(x => x.StreamManagementId == streamManagementId);

                // コメント取得処理
                await _queue.QueueBackgroundWorkItemAsync(async (CancellationToken token) =>
                {
                    Models.CommentFunc commentFunc = new Models.CommentFunc();

                    try
                    {
                        if (streamUrl.IndexOf("youtube.com") > -1)
                        {
                            _logger.LogInformation("YouTube Liveへ接続します　No:{0}", streamNo);
                            commentFunc.Comment = new YouTube.Comment(tmpInt, streamUrl);
                        }
                        else if (streamUrl.IndexOf("nicovideo.jp") > -1)
                        {
                            _logger.LogInformation("ニコニコ生放送へ接続します　No:{0}", streamNo);
                            commentFunc.Comment = new NicoNico.Comment(tmpInt, streamUrl);
                        }

                        _logger.LogInformation("配信枠の情報を取得します　No:{0}", streamNo);
                        result = await commentFunc.Comment.GetStreamInfo(commentFunc.cancellationTokenSource.Token);

                        if (result)
                        {
                            // 配信情報更新
                            commentFunc.Comment.StreamInfo.StreamId = streamManagementInfo.StreamId;
                            await Common.Controllers.StreamInfoApi.UpdateStreamInfo(commentFunc.Comment.StreamInfo);
                            if (commentFunc.Comment.StreamInfo.LiveStatus > 0)
                            {
                                _logger.LogInformation("配信枠に接続します　No:{0}", streamNo);
                                result = await commentFunc.Comment.StartAsync(commentFunc.cancellationTokenSource.Token);
                                streamManagementInfo.ControlRequest = result ? 1 : 2;
                                streamManagementInfo.IsConnected = result;
                                await Common.Controllers.StreamManagementApi.SetConnectionStatus(streamManagementInfo);

                                if (result)
                                {
                                    await Common.Controllers.StreamManagementApi.SendConnectionStartStatusToWebSocket(streamManagementInfo, result);
                                    _logger.LogInformation("配信サイトに接続しました　No:{0}", streamNo);

                                    while (true)
                                    {
                                        StreamManagementInfo tmpStreamManagementInfo = await Common.Controllers.StreamManagementApi.GetData(streamManagementInfo.StreamManagementId);

                                        if (tmpStreamManagementInfo.ControlRequest is not null)
                                        {
                                            if (!tmpStreamManagementInfo.IsConnected || tmpStreamManagementInfo.ControlRequest == 2) { break; }
                                        }
                                        else
                                        {
                                            if (!tmpStreamManagementInfo.IsConnected) { break; }
                                        }
                                        await Task.Delay(1000);
                                    }
                                }
                                else
                                {
                                    string message = "配信枠への接続に失敗しました";
                                    _logger.LogError(message + "　No:{0}", streamNo);
                                    await Common.Controllers.StreamManagementApi.SendConnectionStartStatusToWebSocket(streamManagementInfo, result, message);
                                }
                                _logger.LogInformation("配信サイトから切断します　No:{0}", streamNo);

                                commentFunc.cancellationTokenSource.Cancel();
                                await commentFunc.Comment.StopAsync();
                                streamManagementInfo.IsConnected = false;
                                await Common.Controllers.StreamManagementApi.SetConnectionStatus(streamManagementInfo);
                                await Common.Controllers.StreamManagementApi.SendConnectionStopStatusToWebSocket(streamManagementInfo);
                            }
                            else
                            {
                                string message = "配信枠が配信状態ではありません";
                                result = false;
                                _logger.LogError(message + "　No:{0}", streamNo);
                                await Common.Controllers.StreamManagementApi.SendConnectionStartStatusToWebSocket(streamManagementInfo, result, message);
                            }
                        }
                        else
                        {
                            string message = "配信枠の情報取得に失敗しました";
                            result = false;
                            _logger.LogError(message + "　No:{0}", streamNo);
                            await Common.Controllers.StreamManagementApi.SendConnectionStartStatusToWebSocket(streamManagementInfo, result, message);
                        }

                        _logger.LogInformation("コメント取得処理が終了しました　No:{0}", streamNo);
                    }
                    catch (Exception ex)
                    {
                        string message = string.Format("チャット情報取得に失敗しました。エラーメッセージ:{0}", ex.Message);
                        result = false;
                        _logger.LogError(ex, message);
                        await Common.Controllers.StreamManagementApi.SendConnectionStartStatusToWebSocket(streamManagementInfo, false);
                    }

                });

                await Clients.All.SendAsync("Starting", streamNo, streamUrl);
            }
            catch (Exception ex)
            {
                string message = string.Format("チャット情報取得開始に失敗しました。エラーメッセージ:{0}", ex.Message);
                result = false;
                _logger.LogError(ex, message);
                await Clients.All.SendAsync("ErrorMessage", "Start", streamNo, message);
            }

            _logger.LogDebug("==============================   End    ==============================");
        }

        /// <summary>
        /// コメント受信停止
        /// </summary>
        /// <param name="streamNo"></param>
        /// <param name="streamManagementId"></param>
        /// <returns></returns>
        public async Task Stop(string streamNo, string streamManagementId)
        {
            bool result = false;
            int tmpInt = -1;
            Models.CommentFunc commentFunc = new Models.CommentFunc();
            _logger.LogDebug("==============================  Start   ==============================");

            try
            {
                // DBのキューを停止
                var streamManagementInfo = _context.StreamManagementInfos.SingleOrDefault(x => x.StreamManagementId == streamManagementId);
                if (streamManagementInfo is not null)
                {
                    await Clients.All.SendAsync("Stopping", streamNo);
                    streamManagementInfo.ControlRequest = 2;
                    this._context.Update(streamManagementInfo);
                    await this._context.SaveChangesAsync();
                }
                else
                {
                    await Clients.All.SendAsync("ErrorMessage", "Stop", streamManagementId, "DBにキューが存在しません。");
                    _logger.LogError("DBにキューが存在しません。ID:{0}", streamManagementId);
                }
            }
            catch (Exception ex)
            {
                string message = string.Format("チャット情報取得停止に失敗しました。エラーメッセージ:{0}", ex.Message);
                _logger.LogError(ex, message);
                await Clients.All.SendAsync("ErrorMessage", "Stop", streamManagementId, message);
            }

            _logger.LogDebug("==============================   End    ==============================");
        }

        /// <summary>
        /// コメント受信停止済み
        /// </summary>
        /// <param name="streamNo"></param>
        /// <returns></returns>
        public async Task Stoped(string streamNo)
        {
            _logger.LogDebug("==============================  Start   ==============================");

            await Clients.All.SendAsync("Stoped", streamNo);

            _logger.LogDebug("==============================   End    ==============================");
        }

        /// <summary>
        /// DBにコメント登録
        /// </summary>
        /// <param name="streamNo"></param>
        /// <param name="commentInfo"></param>
        /// <returns></returns>
        public async Task PostCommentToDb(int streamNo, CommentInfo commentInfo)
        {
            _logger.LogTrace("==============================  Start   ==============================");

            try
            {
                _logger.LogTrace("・登録情報");
                _logger.LogTrace("----------------------------------------------------------------------");
                _logger.LogTrace("配信ID        :{0}", commentInfo.StreamInfoId);
                _logger.LogTrace("タイムスタンプ:{0}", commentInfo.TimeStampUSec);
                _logger.LogTrace("コメントID    :{0}", commentInfo.CommentId);
                _logger.LogTrace("配信管理ID    :{0}", commentInfo.StreamManagementId);
                _logger.LogTrace("配信サイトID  :{0}", commentInfo.StreamSiteId);
                _logger.LogTrace("ユーザーID    :{0}", commentInfo.UserId);
                _logger.LogTrace("ユーザー名    :{0}", commentInfo.UserName);
                _logger.LogTrace("コメント      :{0}", commentInfo.CommentText);
                _logger.LogTrace("----------------------------------------------------------------------");

                var tmpCommentInfo = this._context.CommentInfos
                    .SingleOrDefault(x =>
                        x.StreamInfoId == commentInfo.StreamInfoId &&
                        x.TimeStampUSec == commentInfo.TimeStampUSec &&
                        x.CommentId == commentInfo.CommentId);

                if (tmpCommentInfo is null)
                {
                    _logger.LogTrace("新規登録");
                    await ReceiveMessage(
                        commentInfo.TimeStampUSec,
                        commentInfo.CommentId,
                        streamNo,
                        commentInfo.UserId,
                        commentInfo.UserName,
                        commentInfo.CommentText);
                    this._context.Add(commentInfo);
                }
                else
                {
                    tmpCommentInfo.StreamInfoId = commentInfo.StreamInfoId;
                    tmpCommentInfo.TimeStampUSec = commentInfo.TimeStampUSec;
                    tmpCommentInfo.CommentId = commentInfo.CommentId;
                    tmpCommentInfo.StreamManagementId = commentInfo.StreamManagementId;
                    tmpCommentInfo.StreamSiteId = commentInfo.StreamSiteId;
                    tmpCommentInfo.UserId = commentInfo.UserId;
                    tmpCommentInfo.UserName = commentInfo.UserName;
                    tmpCommentInfo.CommentText = commentInfo.CommentText;
                    this._context.Update(tmpCommentInfo);
                }

                await this._context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                string message = string.Format("チャットのDB登録に失敗しました。エラーメッセージ:{0}", ex.Message);
                _logger.LogError(ex, message);
                await Clients.All.SendAsync("ErrorMessage", "PostCommentToDb", commentInfo.CommentId, message);
                _logger.LogDebug("----------------------------------------------------------------------");
                _logger.LogDebug("・登録情報");
                _logger.LogTrace("----------------------------------------------------------------------");
                _logger.LogTrace("配信ID        :{0}", commentInfo.StreamInfoId);
                _logger.LogTrace("タイムスタンプ:{0}", commentInfo.TimeStampUSec);
                _logger.LogTrace("コメントID    :{0}", commentInfo.CommentId);
                _logger.LogTrace("配信管理ID    :{0}", commentInfo.StreamManagementId);
                _logger.LogTrace("配信サイトID  :{0}", commentInfo.StreamSiteId);
                _logger.LogTrace("ユーザーID    :{0}", commentInfo.UserId);
                _logger.LogTrace("ユーザー名    :{0}", commentInfo.UserName);
                _logger.LogTrace("コメント      :{0}", commentInfo.CommentText);
                _logger.LogTrace("----------------------------------------------------------------------");
            }

            _logger.LogTrace("==============================   End    ==============================");
        }

        /// <summary>
        /// DBにコメント一括登録
        /// </summary>
        /// <param name="streamNo"></param>
        /// <param name="commentInfoList"></param>
        /// <returns></returns>
        public async Task PostCommentListToDb(int streamNo, List<CommentInfo> commentInfoList)
        {
            _logger.LogTrace("==============================  Start   ==============================");

            try
            {
                _logger.LogTrace("データ件数:{0}", commentInfoList.Count);

                foreach (var commentInfo in commentInfoList.Select((Value, Index) => (Value, Index)))
                {
                    _logger.LogTrace("----------------------------------------------------------------------");
                    _logger.LogTrace("[{0}]登録情報", commentInfo.Index);
                    _logger.LogTrace("----------------------------------------------------------------------");
                    _logger.LogTrace("[{0}]配信ID        :{1}", commentInfo.Index, commentInfo.Value.StreamInfoId);
                    _logger.LogTrace("[{0}]タイムスタンプ:{1}", commentInfo.Index, commentInfo.Value.TimeStampUSec);
                    _logger.LogTrace("[{0}]コメントID    :{1}", commentInfo.Index, commentInfo.Value.CommentId);
                    _logger.LogTrace("[{0}]配信管理ID    :{1}", commentInfo.Index, commentInfo.Value.StreamManagementId);
                    _logger.LogTrace("[{0}]配信サイトID  :{1}", commentInfo.Index, commentInfo.Value.StreamSiteId);
                    _logger.LogTrace("[{0}]ユーザーID    :{1}", commentInfo.Index, commentInfo.Value.UserId);
                    _logger.LogTrace("[{0}]ユーザー名    :{1}", commentInfo.Index, commentInfo.Value.UserName);
                    _logger.LogTrace("[{0}]コメント      :{1}", commentInfo.Index, commentInfo.Value.CommentText);
                    _logger.LogTrace("----------------------------------------------------------------------");

                    var tmpCommentInfo = this._context.CommentInfos
                    .SingleOrDefault(x =>
                    x.StreamInfoId == commentInfo.Value.StreamInfoId &&
                            x.TimeStampUSec == commentInfo.Value.TimeStampUSec &&
                            x.CommentId == commentInfo.Value.CommentId);

                    if (tmpCommentInfo is null)
                    {
                        _logger.LogTrace("[{0}]新規登録", commentInfo.Index);
                        await ReceiveMessage(
                            commentInfo.Value.TimeStampUSec,
                            commentInfo.Value.CommentId,
                            streamNo,
                            commentInfo.Value.UserId,
                            commentInfo.Value.UserName,
                            commentInfo.Value.CommentText);
                        this._context.Add(commentInfo.Value);
                    }
                    else
                    {
                        _logger.LogTrace("[{0}]更新登録", commentInfo.Index);
                        tmpCommentInfo.StreamInfoId = commentInfo.Value.StreamInfoId;
                        tmpCommentInfo.TimeStampUSec = commentInfo.Value.TimeStampUSec;
                        tmpCommentInfo.CommentId = commentInfo.Value.CommentId;
                        tmpCommentInfo.StreamManagementId = commentInfo.Value.StreamManagementId;
                        tmpCommentInfo.StreamSiteId = commentInfo.Value.StreamSiteId;
                        tmpCommentInfo.UserId = commentInfo.Value.UserId;
                        tmpCommentInfo.UserName = commentInfo.Value.UserName;
                        tmpCommentInfo.CommentText = commentInfo.Value.CommentText;
                        this._context.Update(tmpCommentInfo);
                    }

                    try
                    {
                        await this._context.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        string message = string.Format("[{1}]チャットのDB登録に失敗しました。エラーメッセージ:{0}", ex.Message, commentInfo.Index);
                        _logger.LogError(ex, message);
                        await Clients.All.SendAsync("ErrorMessage", "PostCommentListToDb", commentInfoList.Count, message);
                        _logger.LogDebug("----------------------------------------------------------------------");
                        _logger.LogDebug("[{0}]登録情報", commentInfo.Index);
                        _logger.LogDebug("----------------------------------------------------------------------");
                        _logger.LogDebug("[{0}]配信ID        :{1}", commentInfo.Index, commentInfo.Value.StreamInfoId);
                        _logger.LogDebug("[{0}]タイムスタンプ:{1}", commentInfo.Index, commentInfo.Value.TimeStampUSec);
                        _logger.LogDebug("[{0}]コメントID    :{1}", commentInfo.Index, commentInfo.Value.CommentId);
                        _logger.LogDebug("[{0}]配信管理ID    :{1}", commentInfo.Index, commentInfo.Value.StreamManagementId);
                        _logger.LogDebug("[{0}]配信サイトID  :{1}", commentInfo.Index, commentInfo.Value.StreamSiteId);
                        _logger.LogDebug("[{0}]ユーザーID    :{1}", commentInfo.Index, commentInfo.Value.UserId);
                        _logger.LogDebug("[{0}]ユーザー名    :{1}", commentInfo.Index, commentInfo.Value.UserName);
                        _logger.LogDebug("[{0}]コメント      :{1}", commentInfo.Index, commentInfo.Value.CommentText);
                        _logger.LogDebug("----------------------------------------------------------------------");

                    }

                }
            }
            catch (Exception ex)
            {
                string message = string.Format("チャットのDB登録に失敗しました。エラーメッセージ:{0}", ex.Message);
                _logger.LogError(ex, message);
                await Clients.All.SendAsync("ErrorMessage", "PostCommentListToDb", commentInfoList.Count, message);
            }

            _logger.LogTrace("==============================   End    ==============================");
        }

        /// <summary>
        /// DBにユーザー登録
        /// </summary>
        /// <param name="userInfo"></param>
        /// <returns></returns>
        public async Task PostUserToDb(UserInfo userInfo)
        {
            _logger.LogTrace("==============================  Start   ==============================");

            try
            {
                _logger.LogTrace("・登録情報");
                _logger.LogTrace("----------------------------------------------------------------------");
                _logger.LogTrace("配信サイトID:{0}", userInfo.StreamSiteId);
                _logger.LogTrace("ユーザーID  :{0}", userInfo.UserId);
                _logger.LogTrace("ユーザー名  :{0}", userInfo.UserName);
                _logger.LogTrace("アイコンURL :{0}", userInfo.IconPath);
                _logger.LogTrace("コメント    :{0}", userInfo.Note);
                _logger.LogTrace("----------------------------------------------------------------------");

                var tmpUserInfo = this._context.UserInfos
                    .SingleOrDefault(x =>
                        x.StreamSiteId == userInfo.StreamSiteId &&
                        x.UserId == userInfo.UserId);

                if (tmpUserInfo is null)
                {
                    _logger.LogTrace("新規登録");
                    userInfo.FastCommentDateTime = DateTime.Now;
                    userInfo.LastCommentDateTime = DateTime.Now;
                    this._context.Add(userInfo);
                }
                else
                {
                    _logger.LogTrace("更新登録");
                    tmpUserInfo.StreamSiteId = userInfo.StreamSiteId;
                    tmpUserInfo.UserId = userInfo.UserId;
                    tmpUserInfo.UserName = userInfo.UserName;
                    tmpUserInfo.IconPath = userInfo.IconPath;
                    tmpUserInfo.FastCommentDateTime = tmpUserInfo.FastCommentDateTime is null ? DateTime.Now : tmpUserInfo.FastCommentDateTime;
                    tmpUserInfo.LastCommentDateTime = DateTime.Now;
                    tmpUserInfo.Note = userInfo.Note;
                    this._context.Update(tmpUserInfo);
                }

                await this._context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                string message = string.Format("ユーザーのDB登録に失敗しました。エラーメッセージ:{0}", ex.Message);
                _logger.LogError(ex, message);
                await Clients.All.SendAsync("ErrorMessage", "PostUserToDb", userInfo.UserId, message);
                _logger.LogDebug("----------------------------------------------------------------------");
                _logger.LogDebug("・登録情報");
                _logger.LogDebug("----------------------------------------------------------------------");
                _logger.LogDebug("配信サイトID:{0}", userInfo.StreamSiteId);
                _logger.LogDebug("ユーザーID  :{0}", userInfo.UserId);
                _logger.LogDebug("ユーザー名  :{0}", userInfo.UserName);
                _logger.LogDebug("アイコンURL :{0}", userInfo.IconPath);
                _logger.LogDebug("コメント    :{0}", userInfo.Note);
                _logger.LogDebug("----------------------------------------------------------------------");
            }

            _logger.LogTrace("==============================   End    ==============================");
        }

        /// <summary>
        /// DBにユーザー一括登録
        /// </summary>
        /// <param name="userInfoList"></param>
        /// <returns></returns>
        public async Task PostUserListToDb(List<UserInfo> userInfoList)
        {
            _logger.LogTrace("==============================  Start   ==============================");

            try
            {
                _logger.LogTrace("データ件数:{0}", userInfoList.Count);

                foreach (var userInfo in userInfoList.Select((Value, Index) => (Value, Index)))
                {
                    _logger.LogTrace("----------------------------------------------------------------------");
                    _logger.LogTrace("[{0}]登録情報", userInfo.Index);
                    _logger.LogTrace("----------------------------------------------------------------------");
                    _logger.LogTrace("[{0}]配信サイトID:{1}", userInfo.Index, userInfo.Value.StreamSiteId);
                    _logger.LogTrace("[{0}]ユーザーID  :{1}", userInfo.Index, userInfo.Value.UserId);
                    _logger.LogTrace("[{0}]ユーザー名  :{1}", userInfo.Index, userInfo.Value.UserName);
                    _logger.LogTrace("[{0}]アイコンURK :{1}", userInfo.Index, userInfo.Value.IconPath);
                    _logger.LogTrace("[{0}]コメント    :{1}", userInfo.Index, userInfo.Value.Note);
                    _logger.LogTrace("----------------------------------------------------------------------");

                    var tmpUserInfo = this._context.UserInfos
                        .SingleOrDefault(x =>
                            x.StreamSiteId == userInfo.Value.StreamSiteId &&
                            x.UserId == userInfo.Value.UserId);

                    if (tmpUserInfo is null)
                    {
                        _logger.LogTrace("[{0}]新規登録", userInfo.Index);
                        userInfo.Value.FastCommentDateTime = DateTime.Now;
                        userInfo.Value.LastCommentDateTime = DateTime.Now;
                        this._context.Add(userInfo.Value);
                    }
                    else
                    {
                        _logger.LogTrace("[{0}]更新登録", userInfo.Index);
                        tmpUserInfo.StreamSiteId = userInfo.Value.StreamSiteId;
                        tmpUserInfo.UserId = userInfo.Value.UserId;
                        tmpUserInfo.UserName = userInfo.Value.UserName;
                        tmpUserInfo.IconPath = userInfo.Value.IconPath;
                        tmpUserInfo.FastCommentDateTime = tmpUserInfo.FastCommentDateTime is null ? DateTime.Now : tmpUserInfo.FastCommentDateTime;
                        tmpUserInfo.LastCommentDateTime = DateTime.Now;
                        tmpUserInfo.Note = userInfo.Value.Note;
                        this._context.Update(tmpUserInfo);
                    }

                    try
                    {
                        await this._context.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        string message = string.Format("[{1}}ユーザーのDB登録に失敗しました。エラーメッセージ:{0}", ex.Message, userInfo.Index);
                        _logger.LogError(ex, message);
                        await Clients.All.SendAsync("ErrorMessage", "PostUserListToDb", userInfoList.Count, message);
                        _logger.LogDebug("----------------------------------------------------------------------");
                        _logger.LogDebug("[{0}]登録情報", userInfo.Index);
                        _logger.LogDebug("----------------------------------------------------------------------");
                        _logger.LogDebug("[{0}]配信サイトID:{1}", userInfo.Index, userInfo.Value.StreamSiteId);
                        _logger.LogDebug("[{0}]ユーザーID  :{1}", userInfo.Index, userInfo.Value.UserId);
                        _logger.LogDebug("[{0}]ユーザー名  :{1}", userInfo.Index, userInfo.Value.UserName);
                        _logger.LogDebug("[{0}]アイコンURK :{1}", userInfo.Index, userInfo.Value.IconPath);
                        _logger.LogDebug("[{0}]コメント    :{1}", userInfo.Index, userInfo.Value.Note);
                        _logger.LogDebug("----------------------------------------------------------------------");
                    }
                }
            }
            catch (Exception ex)
            {
                string message = string.Format("ユーザーのDB登録に失敗しました。エラーメッセージ:{0}", ex.Message);
                _logger.LogError(ex, message);
                await Clients.All.SendAsync("ErrorMessage", "PostUserListToDb", userInfoList.Count, message);
            }

            _logger.LogTrace("==============================   End    ==============================");
        }

        /// <summary>
        /// DBにコメント登録
        /// </summary>
        /// <param name="streamNo"></param>
        /// <param name="giftInfo"></param>
        /// <returns></returns>
        public async Task PostGiftToDb(int streamNo, GiftInfo giftInfo)
        {
            _logger.LogTrace("==============================  Start   ==============================");

            try
            {
                _logger.LogTrace("・登録情報");
                _logger.LogTrace("----------------------------------------------------------------------");
                _logger.LogTrace("配信ID        :{0}", giftInfo.StreamInfoId);
                _logger.LogTrace("タイムスタンプ:{0}", giftInfo.TimeStampUSec);
                _logger.LogTrace("コメントID    :{0}", giftInfo.CommentId);
                _logger.LogTrace("配信管理ID    :{0}", giftInfo.StreamManagementId);
                _logger.LogTrace("配信サイトID  :{0}", giftInfo.StreamSiteId);
                _logger.LogTrace("ユーザーID    :{0}", giftInfo.UserId);
                _logger.LogTrace("ユーザー名    :{0}", giftInfo.UserName);
                _logger.LogTrace("ギフト種別    :{0}", giftInfo.GiftType);
                _logger.LogTrace("ギフト金額    :{0}", giftInfo.GiftValue);
                _logger.LogTrace("コメント      :{0}", giftInfo.CommentText);
                _logger.LogTrace("----------------------------------------------------------------------");

                var tmpGiftInfo = this._context.GiftInfos
                    .SingleOrDefault(x =>
                        x.StreamInfoId == giftInfo.StreamInfoId &&
                        x.TimeStampUSec == giftInfo.TimeStampUSec &&
                        x.CommentId == giftInfo.CommentId);

                if (tmpGiftInfo is null)
                {
                    _logger.LogTrace("新規登録");
                    await ReceiveGift(
                        giftInfo.TimeStampUSec,
                        giftInfo.CommentId,
                        streamNo,
                        giftInfo.UserId,
                        giftInfo.UserName,
                        giftInfo.GiftType,
                        giftInfo.GiftValue,
                        giftInfo.CommentText);
                    this._context.Add(giftInfo);
                }
                else
                {
                    tmpGiftInfo.StreamInfoId = giftInfo.StreamInfoId;
                    tmpGiftInfo.TimeStampUSec = giftInfo.TimeStampUSec;
                    tmpGiftInfo.CommentId = giftInfo.CommentId;
                    tmpGiftInfo.StreamManagementId = giftInfo.StreamManagementId;
                    tmpGiftInfo.StreamSiteId = giftInfo.StreamSiteId;
                    tmpGiftInfo.UserId = giftInfo.UserId;
                    tmpGiftInfo.UserName = giftInfo.UserName;
                    tmpGiftInfo.CommentText = giftInfo.CommentText;
                    this._context.Update(tmpGiftInfo);
                }

                await this._context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                string message = string.Format("チャットのDB登録に失敗しました。エラーメッセージ:{0}", ex.Message);
                _logger.LogError(ex, message);
                await Clients.All.SendAsync("ErrorMessage", "PostGiftToDb", giftInfo.CommentId, message);
                _logger.LogDebug("・登録情報");
                _logger.LogDebug("----------------------------------------------------------------------");
                _logger.LogDebug("配信ID        :{0}", giftInfo.StreamInfoId);
                _logger.LogDebug("タイムスタンプ:{0}", giftInfo.TimeStampUSec);
                _logger.LogDebug("コメントID    :{0}", giftInfo.CommentId);
                _logger.LogDebug("配信管理ID    :{0}", giftInfo.StreamManagementId);
                _logger.LogDebug("配信サイトID  :{0}", giftInfo.StreamSiteId);
                _logger.LogDebug("ユーザーID    :{0}", giftInfo.UserId);
                _logger.LogDebug("ユーザー名    :{0}", giftInfo.UserName);
                _logger.LogDebug("ギフト種別    :{0}", giftInfo.GiftType);
                _logger.LogDebug("ギフト金額    :{0}", giftInfo.GiftValue);
                _logger.LogDebug("コメント      :{0}", giftInfo.CommentText);
                _logger.LogDebug("----------------------------------------------------------------------");
            }

            _logger.LogTrace("==============================   End    ==============================");
        }

        /// <summary>
        /// DBにコメント一括登録
        /// </summary>
        /// <param name="streamNo"></param>
        /// <param name="giftInfoList"></param>
        /// <returns></returns>
        public async Task PostGiftListToDb(int streamNo, List<GiftInfo> giftInfoList)
        {
            _logger.LogTrace("==============================  Start   ==============================");

            try
            {
                _logger.LogTrace("データ件数:{0}", giftInfoList.Count);

                foreach (var giftInfo in giftInfoList.Select((Value, Index) => (Value, Index)))
                {
                    _logger.LogTrace("----------------------------------------------------------------------");
                    _logger.LogTrace("[{0}]登録情報", giftInfo.Index);
                    _logger.LogTrace("----------------------------------------------------------------------");
                    _logger.LogTrace("[{0}]配信ID        :{1}", giftInfo.Index, giftInfo.Value.StreamInfoId);
                    _logger.LogTrace("[{0}]タイムスタンプ:{1}", giftInfo.Index, giftInfo.Value.TimeStampUSec);
                    _logger.LogTrace("[{0}]コメントID    :{1}", giftInfo.Index, giftInfo.Value.CommentId);
                    _logger.LogTrace("[{0}]配信管理ID    :{1}", giftInfo.Index, giftInfo.Value.StreamManagementId);
                    _logger.LogTrace("[{0}]配信サイトID  :{1}", giftInfo.Index, giftInfo.Value.StreamSiteId);
                    _logger.LogTrace("[{0}]ユーザーID    :{1}", giftInfo.Index, giftInfo.Value.UserId);
                    _logger.LogTrace("[{0}]ユーザー名    :{1}", giftInfo.Index, giftInfo.Value.UserName);
                    _logger.LogTrace("[{0}]ギフト種別    :{1}", giftInfo.Index, giftInfo.Value.GiftType);
                    _logger.LogTrace("[{0}]ギフト金額    :{1}", giftInfo.Index, giftInfo.Value.GiftValue);
                    _logger.LogTrace("[{0}]コメント      :{1}", giftInfo.Index, giftInfo.Value.CommentText);
                    _logger.LogTrace("----------------------------------------------------------------------");

                    var tmpGiftInfo = this._context.GiftInfos
                    .SingleOrDefault(x =>
                    x.StreamInfoId == giftInfo.Value.StreamInfoId &&
                            x.TimeStampUSec == giftInfo.Value.TimeStampUSec &&
                            x.CommentId == giftInfo.Value.CommentId);

                    if (tmpGiftInfo is null)
                    {
                        _logger.LogTrace("[{0}]新規登録", giftInfo.Index);
                        await ReceiveGift(
                            giftInfo.Value.TimeStampUSec,
                            giftInfo.Value.CommentId,
                            streamNo,
                            giftInfo.Value.UserId,
                            giftInfo.Value.UserName,
                            giftInfo.Value.GiftType,
                            giftInfo.Value.GiftValue,
                            giftInfo.Value.CommentText);
                        this._context.Add(giftInfo.Value);
                    }
                    else
                    {
                        _logger.LogTrace("[{0}]更新登録", giftInfo.Index);
                        tmpGiftInfo.StreamInfoId = giftInfo.Value.StreamInfoId;
                        tmpGiftInfo.TimeStampUSec = giftInfo.Value.TimeStampUSec;
                        tmpGiftInfo.CommentId = giftInfo.Value.CommentId;
                        tmpGiftInfo.StreamManagementId = giftInfo.Value.StreamManagementId;
                        tmpGiftInfo.StreamSiteId = giftInfo.Value.StreamSiteId;
                        tmpGiftInfo.UserId = giftInfo.Value.UserId;
                        tmpGiftInfo.UserName = giftInfo.Value.UserName;
                        tmpGiftInfo.CommentText = giftInfo.Value.CommentText;
                        this._context.Update(tmpGiftInfo);
                    }

                    try
                    {
                        await this._context.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        string message = string.Format("チャットのDB登録に失敗しました。エラーメッセージ:{0}", ex.Message);
                        _logger.LogError(ex, message);
                        await Clients.All.SendAsync("ErrorMessage", "PostGiftListToDb", giftInfoList.Count, message);
                        _logger.LogDebug("----------------------------------------------------------------------");
                        _logger.LogDebug("[{0}]登録情報", giftInfo.Index);
                        _logger.LogDebug("----------------------------------------------------------------------");
                        _logger.LogDebug("[{0}]配信ID        :{1}", giftInfo.Index, giftInfo.Value.StreamInfoId);
                        _logger.LogDebug("[{0}]タイムスタンプ:{1}", giftInfo.Index, giftInfo.Value.TimeStampUSec);
                        _logger.LogDebug("[{0}]コメントID    :{1}", giftInfo.Index, giftInfo.Value.CommentId);
                        _logger.LogDebug("[{0}]配信管理ID    :{1}", giftInfo.Index, giftInfo.Value.StreamManagementId);
                        _logger.LogDebug("[{0}]配信サイトID  :{1}", giftInfo.Index, giftInfo.Value.StreamSiteId);
                        _logger.LogDebug("[{0}]ユーザーID    :{1}", giftInfo.Index, giftInfo.Value.UserId);
                        _logger.LogDebug("[{0}]ユーザー名    :{1}", giftInfo.Index, giftInfo.Value.UserName);
                        _logger.LogDebug("[{0}]ギフト種別    :{1}", giftInfo.Index, giftInfo.Value.GiftType);
                        _logger.LogDebug("[{0}]ギフト金額    :{1}", giftInfo.Index, giftInfo.Value.GiftValue);
                        _logger.LogDebug("[{0}]コメント      :{1}", giftInfo.Index, giftInfo.Value.CommentText);
                        _logger.LogDebug("----------------------------------------------------------------------");
                    }
                }
            }
            catch (Exception ex)
            {
                string message = string.Format("チャットのDB登録に失敗しました。エラーメッセージ:{0}", ex.Message);
                _logger.LogError(ex, message);
                await Clients.All.SendAsync("ErrorMessage", "PostGiftListToDb", giftInfoList.Count, message);
            }

            _logger.LogTrace("==============================   End    ==============================");
        }

        /// <summary>
        /// クライアント接続時(デバッグ出力用)
        /// </summary>
        /// <returns></returns>
        public override Task OnConnectedAsync()
        {
            _logger.LogDebug($"{Context.ConnectionId} connected");
            return base.OnConnectedAsync();
        }

        /// <summary>
        /// クライアント切断時(デバッグ出力用)
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public override async Task OnDisconnectedAsync(Exception e)
        {
            _logger.LogDebug($"Disconnected {e?.Message} {Context.ConnectionId}");
            await base.OnDisconnectedAsync(e);
        }

    }
}
