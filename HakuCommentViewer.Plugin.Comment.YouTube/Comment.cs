using System;
using System.Text;
using System.Threading;

using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Newtonsoft.Json.Linq;

using HakuCommentViewer.Common.Controllers;
using HakuCommentViewer.Common.Models;
using HakuCommentViewer.Plugin;
using HakuCommentViewer.Plugin.Interface;
using HakuCommentViewer.Plugin.Models;
using System.Collections.Generic;

namespace HakuCommentViewer.Plugin.Comment.YouTube
{
    /// <summary>
    /// コメント取得クラス
    /// </summary>
    public class Comment : IComment
    {
        #region 非公開メンバ
        /// <summary>
        /// NLogロガー
        /// </summary>
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        /// <summary>
        /// キャンセルトークンソース
        /// </summary>
        private CancellationTokenSource _cancellationTokenSource = new();

        /// <summary>
        /// 非同期処理実行インスタンス
        /// </summary>
        private Task _task;

        /// <summary>
        /// コメント登録用オブジェクトインスタンス
        /// </summary>
        private CommentApi _commentApi = new CommentApi();
        #endregion

        #region 公開メンバ
        /// <summary>
        /// プラグイン読込状態
        /// </summary>
        public bool IsLoaded { get; private set; }

        /// <summary>
        /// プラグイン実行状態
        /// </summary>
        public bool IsRunning { get; private set; }

        /// <summary>
        /// プラグイン情報
        /// </summary>
        public PluginInfo PluginInfo { get; private set; }

        /// <summary>
        /// 配信管理情報
        /// </summary>
        public StreamManagementInfo StreamManagementInfo { get; set; }

        /// <summary>
        /// 配信管理情報
        /// </summary>
        public StreamInfo StreamInfo { get; private set; }
        #endregion

        #region イベントハンドラ
        /// <summary>
        /// 受信受信イベントハンドラ
        /// </summary>
        public event EventHandler<GetNewCommentArgs> GetCommentHandler;
        /// <summary>
        /// エラー発生イベントハンドラ
        /// </summary>
        public event EventHandler<OnExcrptionArgs> OnExcrptionHandler;
        #endregion

        /// <summary>
        /// コンストラクター
        /// </summary>
        /// <param name="no"></param>
        /// <param name="url"></param>
        public Comment(int no, string url)
        {
            // プラグイン情報設定
            this.PluginInfo = new PluginInfo();
            this.PluginInfo.Id = "FDB0DE51-D95B-421E-8115-F823C384B9A2";
            this.PluginInfo.Name = "コメント取得/送信プラグイン：YouTube";
            this.PluginInfo.Author = "風丸　白";
            this.PluginInfo.Description = "YouTube Liveのコメントを取得/送信するプラグインです。※Comment送信機能は未実装。";
            this.PluginInfo.Status = Enums.PluginStatus.Loaded;
            this.PluginInfo.Type = Enums.PluginType.Comment;

            // 配信管理情報設定
            this.StreamManagementInfo = new StreamManagementInfo();
            this.StreamManagementInfo.StreamManagementId = Guid.NewGuid().ToString();
            this.StreamManagementInfo.StreamNo = no;
            this.StreamManagementInfo.StreamId = "%NULL%";
            this.StreamManagementInfo.IsConnected = false;
            this.StreamManagementInfo.UseCommentGenerator = false;
            this.StreamManagementInfo.UseNarrator = false;

            // 配信情報設定
            this.StreamInfo = new StreamInfo();
            this.StreamInfo.StreamSiteId = this.PluginInfo.Id;
            this.StreamInfo.StreamName = "%NULL%";
            this.StreamInfo.StreamUrl = url;
            this.StreamInfo.CommentRoomId = "%NULL%";
            this.StreamInfo.ViewerCount = -1;
            this.StreamInfo.CommentCount = -1;
            this.StreamInfo.StartDateTime = DateTime.MinValue;
            this.StreamInfo.EndDateTime = DateTime.MinValue;
            this.StreamInfo.LiveStatus = -1;
        }

        /// <summary>
        /// 配信情報取得処理
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<bool> GetStreamInfo(CancellationToken cancellationToken)
        {
            bool returnVal = false;
            JObject jsondata = null;
            logger.Debug("========== Func Start! ==================================================");

            // キャンセルトークンの設定
            if (cancellationToken == CancellationToken.None)
            {
                cancellationToken = this._cancellationTokenSource.Token;
            }

            // 配信情報取得
            jsondata = await GetStreamInfoJson(this.StreamInfo.StreamUrl);

            if (jsondata is not null)
            {
                // 配信ID取得
                this.StreamInfo.StreamPageId = GetStreamID(jsondata);
                // 配信タイトル取得
                this.StreamInfo.StreamName = GetStreamTitle(jsondata);
                // チャットルームID取得
                this.StreamInfo.CommentRoomId = GetChatRoomID(jsondata);
                // 視聴者数取得
                this.StreamInfo.ViewerCount = GetViewerCount(jsondata);
                // 配信開始日時取得
                this.StreamInfo.StartDateTime = GetLiveStartTime(jsondata);

                // 配信状態取得
                if (GetLiveStatus(jsondata))
                {
                    logger.Info("配信名[{0}]：視聴者数[{1}]：配信中", this.StreamInfo.StreamName, this.StreamInfo.ViewerCount);
                    this.StreamInfo.LiveStatus = 1;
                }
                else
                {
                    logger.Info("配信名[{0}]：視聴者数[{1}]：未配信", this.StreamInfo.StreamName, this.StreamInfo.ViewerCount);
                    this.StreamInfo.LiveStatus = 0;
                }

                returnVal = string.IsNullOrWhiteSpace(this.StreamInfo.CommentRoomId) ? false : true;
            }
            else
            {
                returnVal = false;
            }

            logger.Debug("========== Func End!　 ==================================================");
            return returnVal;
        }

        /// <summary>
        /// コメント取得開始(非同期)
        /// </summary>
        /// <returns></returns>
        public async Task<bool> StartAsync(CancellationToken cancellationToken)
        {
            bool returnVal = false;
            logger.Debug("========== Func Start! ==================================================");

            if (cancellationToken == CancellationToken.None)
            {
                cancellationToken = this._cancellationTokenSource.Token;
            }

            returnVal = await GetStreamInfo(cancellationToken);

            this._task = this.CommentGetLoop(cancellationToken);

            logger.Debug("========== Func End!　 ==================================================");
            return returnVal;
        }

        /// <summary>
        /// コメント取得停止
        /// </summary>
        /// <returns></returns>
        public Task StopAsync()
        {
            this._cancellationTokenSource.Cancel();
            return this._task;
        }

        /// <summary>
        /// 配信情報Json取得
        /// </summary>
        /// <returns></returns>
        private async Task<JObject> GetStreamInfoJson(string url)
        {
            JObject returnVal = null;
            logger.Debug("========== Func Start! ==================================================");
            logger.Debug("取得先URL：" + url);

            try
            {
                var responseData = default(IHtmlDocument);

                // スクレイピング
                using (var client = new HttpClient())
                using (var stream = await client.GetStreamAsync(new Uri(url)))
                {
                    var parser = new HtmlParser();
                    responseData = parser.ParseDocument(stream);
                }

                var scriptDocs = responseData.GetElementsByTagName("script");
                foreach (var scriptDoc in scriptDocs)
                {
                    // logger.Trace("html：" + scriptDoc.InnerHtml);
                    if (scriptDoc.InnerHtml.IndexOf("var ytInitialData") != -1)
                    {
                        logger.Debug("検索対象発見");
                        // logger.Trace("html:\r\n" + scriptDoc.InnerHtml);

                        string jsonString = scriptDoc.InnerHtml.Replace("var ytInitialData = ", "").Replace("};", "}");
                        // logger.Trace("json:\n" + jsonString);
                        returnVal = JObject.Parse(jsonString);

                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "配信情報の取得でエラーが発生しました。エラーメッセージ:{0}", ex.Message);
                logger.Debug(ex.StackTrace);
                returnVal = null;
            }

            logger.Debug("========== Func End!　 ==================================================");
            return returnVal;
        }

        /// <summary>
        /// JObjectから配信IDを取得する
        /// </summary>
        /// <param name="jsonObject"></param>
        private string GetStreamID(JObject jsonObject)
        {
            string returnVal = "";
            logger.Debug("========== Func Start! ==================================================");

            try
            {
                var contents = jsonObject["contents"];
                var twoColumnWatchNextResults = contents["twoColumnWatchNextResults"];
                var results = twoColumnWatchNextResults["results"]["results"];
                var innerContents = results["contents"];
                var videoPrimaryInfoRenderer = innerContents[0]["videoPrimaryInfoRenderer"];
                var videoId = videoPrimaryInfoRenderer["updatedMetadataEndpoint"]["updatedMetadataEndpoint"]["videoId"];

                string idString = videoId.ToString();

                if (idString is not null)
                {
                    logger.Debug("配信ID" + idString);

                    returnVal = idString;
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "配信IDの取得でエラーが発生しました。エラーメッセージ:{0}", ex.Message);
                logger.Debug(ex.StackTrace);
                returnVal = "";
            }

            logger.Debug("========== Func End!　 ==================================================");
            return returnVal;
        }

        /// <summary>
        /// JObjectからチャットルームIDを取得する
        /// </summary>
        /// <param name="jsonObject"></param>
        private string GetChatRoomID(JObject jsonObject)
        {
            string returnVal = "";
            logger.Debug("========== Func Start! ==================================================");

            try
            {
                var contents = jsonObject["contents"];
                var twoColumnWatchNextResults = contents["twoColumnWatchNextResults"];
                var conversationBar = twoColumnWatchNextResults["conversationBar"];
                var liveChatRenderer = conversationBar["liveChatRenderer"];
                var continuations = liveChatRenderer["continuations"];
                var reloadContinuationData = continuations[0]["reloadContinuationData"];
                var continuation = reloadContinuationData["continuation"];

                string idString = continuation.ToString();

                if (idString is not null)
                {
                    logger.Debug("チャットルームID" + idString);

                    returnVal = idString;
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "チャットルームIDの取得でエラーが発生しました。エラーメッセージ:{0}", ex.Message);
                logger.Debug(ex.StackTrace);
                returnVal = "";
            }

            logger.Debug("========== Func End!　 ==================================================");
            return returnVal;
        }

        /// <summary>
        /// JObjectから配信タイトルを取得する
        /// </summary>
        /// <param name="jsonObject"></param>
        private string GetStreamTitle(JObject jsonObject)
        {
            string returnVal = "";
            logger.Debug("========== Func Start! ==================================================");

            try
            {
                var contents = jsonObject["contents"];
                var twoColumnWatchNextResults = contents["twoColumnWatchNextResults"];
                var results = twoColumnWatchNextResults["results"]["results"];
                var innerContents = results["contents"];
                var videoPrimaryInfoRenderer = innerContents[0]["videoPrimaryInfoRenderer"];
                var title = videoPrimaryInfoRenderer["title"]["runs"][0]["text"];

                string titleString = title.ToString();

                if (titleString is not null)
                {
                    logger.Debug("配信タイトル" + titleString);

                    returnVal = titleString;
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "配信タイトルの取得でエラーが発生しました。エラーメッセージ:{0}", ex.Message);
                logger.Debug(ex.StackTrace);
                returnVal = "";
            }

            logger.Debug("========== Func End!　 ==================================================");
            return returnVal;
        }

        /// <summary>
        /// 視聴者数を取得する
        /// </summary>
        /// <param name="jsonObject"></param>
        /// <returns></returns>
        private int GetViewerCount(JObject jsonObject)
        {
            int returnVal = -1;
            logger.Debug("========== Func Start! ==================================================");

            try
            {
                int tempVal = 0;
                var contents = jsonObject["contents"];
                var twoColumnWatchNextResults = contents["twoColumnWatchNextResults"];
                var results = twoColumnWatchNextResults["results"]["results"];
                var innerContents = results["contents"];
                var videoPrimaryInfoRenderer = innerContents[0]["videoPrimaryInfoRenderer"];
                var viewCount = videoPrimaryInfoRenderer["viewCount"]["videoViewCountRenderer"]["originalViewCount"];

                if (int.TryParse(viewCount.ToString(), out tempVal))
                {
                    logger.Debug("視聴者数" + tempVal + "人");
                    returnVal = tempVal;
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "視聴者数の取得でエラーが発生しました。エラーメッセージ:{0}", ex.Message);
                logger.Debug(ex.StackTrace);
                returnVal = -1;
            }

            logger.Debug("========== Func End!　 ==================================================");
            return returnVal;
        }

        /// <summary>
        /// 配信状態を取得する
        /// </summary>
        /// <param name="jsonObject"></param>
        /// <returns></returns>
        private bool GetLiveStatus(JObject jsonObject)
        {
            bool returnVal = false;
            logger.Debug("========== Func Start! ==================================================");

            try
            {
                bool tempVal = false;
                var contents = jsonObject["contents"];
                var twoColumnWatchNextResults = contents["twoColumnWatchNextResults"];
                var results = twoColumnWatchNextResults["results"]["results"];
                var innerContents = results["contents"];
                var videoPrimaryInfoRenderer = innerContents[0]["videoPrimaryInfoRenderer"];
                var isLive = videoPrimaryInfoRenderer["viewCount"]["videoViewCountRenderer"]["isLive"];

                if (isLive is not null)
                {
                    if (bool.TryParse(isLive.ToString(), out tempVal))
                    {
                        logger.Debug("配信ステータス" + tempVal);
                        returnVal = tempVal;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "配信ステータスの取得でエラーが発生しました。エラーメッセージ:{0}", ex.Message);
                logger.Debug(ex.StackTrace);
                returnVal = false;
            }

            logger.Debug("========== Func End!　 ==================================================");
            return returnVal;
        }

        /// <summary>
        /// 配信時間を取得する
        /// </summary>
        /// <param name="jsonObject"></param>
        /// <returns></returns>
        private DateTime GetLiveStartTime(JObject jsonObject)
        {
            DateTime returnVal = DateTime.MinValue;
            logger.Debug("========== Func Start! ==================================================");

            try
            {
                DateTime tempVal = DateTime.Now;
                var contents = jsonObject["contents"];
                var twoColumnWatchNextResults = contents["twoColumnWatchNextResults"];
                var results = twoColumnWatchNextResults["results"]["results"];
                var innerContents = results["contents"];
                var videoPrimaryInfoRenderer = innerContents[0]["videoPrimaryInfoRenderer"];
                var simpleText = videoPrimaryInfoRenderer["dateText"]["simpleText"];

                if (simpleText.ToString().IndexOf("前にライブ配信開始") != -1)
                {
                    int tempInt = 0;
                    if (simpleText.ToString().IndexOf("秒") != -1)
                    {
                        if (int.TryParse(simpleText.ToString().Replace(" 秒前にライブ配信開始", ""), out tempInt))
                        {
                            tempVal.AddSeconds(tempInt * -1);
                        }
                    }
                    else if (simpleText.ToString().IndexOf("分") != -1)
                    {
                        if (int.TryParse(simpleText.ToString().Replace(" 分前にライブ配信開始", ""), out tempInt))
                        {
                            tempVal.AddMinutes(tempInt * -1);
                        }
                    }
                    else if (simpleText.ToString().IndexOf("時間") != -1)
                    {
                        if (int.TryParse(simpleText.ToString().Replace(" 時間秒前にライブ配信開始", ""), out tempInt))
                        {
                            tempVal.AddHours(tempInt * -1);
                        }
                    }
                    else
                    {
                        logger.Warn("想定していない時間単位です。表記:{0}", simpleText.ToString());
                        tempVal = DateTime.MinValue;
                    }
                }
                else if (simpleText.ToString().IndexOf("ライブ配信開始日") != -1)
                {
                    if (!DateTime.TryParse(simpleText.ToString().Replace("ライブ配信開始日: ", ""), out tempVal))
                    {
                        logger.Warn("想定していない表記です。表記:{0}", simpleText.ToString());
                        tempVal = DateTime.MinValue;
                    }
                }
                else
                {
                    logger.Warn("想定していない表記です。表記:{0}", simpleText.ToString());
                    tempVal = DateTime.MinValue;
                }

                if (tempVal != DateTime.MinValue)
                {
                    logger.Debug("配信開始日時" + tempVal);
                    returnVal = tempVal;
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "配信開始日時の取得でエラーが発生しました。エラーメッセージ:{0}", ex.Message);
                logger.Debug(ex.StackTrace);
                returnVal = DateTime.MinValue;
            }

            logger.Debug("========== Func End!　 ==================================================");
            return returnVal;
        }

        /// <summary>
        /// コメント取得ループ
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task CommentGetLoop(CancellationToken cancellationToken)
        {
            logger.Debug("========== Func Start! ==================================================");

            try
            {
                this._commentApi.RetryCount = 5;

                while (!cancellationToken.IsCancellationRequested)
                {
                    // コメントJSON取得
                    (int errorCode, JObject jsonObject) = await GetChatData(this.StreamInfo.CommentRoomId, "3c8Rlf9NJOk");

                    if (errorCode > -1)
                    {
                        List<CommentInfo> commentInfoList = new List<CommentInfo>();
                        List<UserInfo> userInfoList = new List<UserInfo>();
                        List<GiftInfo> giftInfoList = new List<GiftInfo>();

                        var continuationContents = jsonObject["continuationContents"];
                        if (continuationContents is null) {
                            logger.Warn("配信中にに存在するデータがありません。");
                            break;
                        }
                        var actions = jsonObject["continuationContents"]["liveChatContinuation"]["actions"];

                        foreach (var action in actions.Select((Value, Index) => (Value, Index)))
                        {
                            CommentInfo commentInfo = new CommentInfo();
                            UserInfo userInfo = new UserInfo();

                            // チャット情報の存在確認
                            var addChatItemAction = action.Value["addChatItemAction"];
                            if (addChatItemAction is null) { continue; }

                            var liveChatTextMessageRenderer = addChatItemAction["item"]["liveChatTextMessageRenderer"];
                            if (liveChatTextMessageRenderer is not null)
                            {
                                // コメント受信処理
                                commentInfo.TimeStampUSec = GetChatTimestamp(liveChatTextMessageRenderer);
                                commentInfo.CommentText = GetChatText(liveChatTextMessageRenderer);
                                commentInfo.UserName = GetUserName(liveChatTextMessageRenderer);

                                userInfo.IconPath = GetUserIcon(liveChatTextMessageRenderer);
                            }
                            else
                            {
                                bool result = false;
                                int purchaseValue = -1;
                                string purchaseAmountText = "", unitString = "";
                                GiftInfo giftInfo = new GiftInfo();

                                liveChatTextMessageRenderer = addChatItemAction["item"]["liveChatTickerPaidMessageItemRenderer"];
                                if (liveChatTextMessageRenderer is not null)
                                {
                                    // スパ茶金額のみのケース
                                    var liveChatPaidMessageRenderer = liveChatTextMessageRenderer["showItemEndpoint"]["showLiveChatItemEndpoint"]["renderer"]["liveChatPaidMessageRenderer"];
                                    if (liveChatPaidMessageRenderer is null) { continue; }

                                    (result, purchaseAmountText, purchaseValue, unitString) = GetPurchaseAmountText(liveChatPaidMessageRenderer);
                                    // if (!result) { continue; }

                                    giftInfo.GiftType = "スーパーチャット";
                                    giftInfo.GiftValue = purchaseValue;
                                    giftInfo.CommentText = $"スパ茶({purchaseAmountText})";
                                    giftInfo.UserId = GetUserId(liveChatTextMessageRenderer);
                                    giftInfo.UserName = GetUserName(liveChatTextMessageRenderer);

                                    commentInfo.TimeStampUSec = GetChatTimestamp(liveChatPaidMessageRenderer);

                                    userInfo.IconPath = GetUserIcon(liveChatTextMessageRenderer);
                                }
                                else
                                {
                                    liveChatTextMessageRenderer = addChatItemAction["item"]["liveChatPaidMessageRenderer"];
                                    if (liveChatTextMessageRenderer is not null)
                                    {
                                        // スパ茶とテキストのケース
                                        (result, purchaseAmountText, purchaseValue, unitString) = GetPurchaseAmountText(liveChatTextMessageRenderer);
                                        // if (!result) { continue; }

                                        giftInfo.GiftType = $"スーパーチャット:{purchaseAmountText}";
                                        giftInfo.GiftValue = purchaseValue;
                                        giftInfo.CommentText = GetChatText(liveChatTextMessageRenderer);
                                        giftInfo.UserId = GetUserId(liveChatTextMessageRenderer);
                                        giftInfo.UserName = GetUserName(liveChatTextMessageRenderer);

                                        commentInfo.TimeStampUSec = GetChatTimestamp(liveChatTextMessageRenderer);

                                        userInfo.IconPath = GetUserIcon(liveChatTextMessageRenderer);
                                    }
                                    else
                                    {
                                        liveChatTextMessageRenderer = addChatItemAction["item"]["liveChatSponsorshipsGiftPurchaseAnnouncementRenderer"];
                                        if (liveChatTextMessageRenderer is not null)
                                        {
                                            // メンバーシップギフトのケース
                                            var liveChatSponsorshipsHeaderRenderer = liveChatTextMessageRenderer["header"]["liveChatSponsorshipsHeaderRenderer"];
                                            if (liveChatSponsorshipsHeaderRenderer is not null)
                                            {
                                                (result, purchaseAmountText, purchaseValue, unitString) = GetMemberGifSendtText(liveChatSponsorshipsHeaderRenderer);
                                                // if (!result) { continue; }

                                                giftInfo.GiftType = $"メンバーシップギフト贈与:{purchaseValue}件";
                                                giftInfo.GiftValue = purchaseValue;
                                                giftInfo.CommentText = purchaseAmountText;
                                                giftInfo.UserId = GetUserId(liveChatTextMessageRenderer);
                                                giftInfo.UserName = GetUserName(liveChatSponsorshipsHeaderRenderer);

                                                commentInfo.TimeStampUSec = GetChatTimestamp(liveChatTextMessageRenderer);

                                                userInfo.IconPath = GetUserIcon(liveChatSponsorshipsHeaderRenderer);
                                            }
                                            else { continue; }
                                        }
                                        else
                                        {
                                            liveChatTextMessageRenderer = addChatItemAction["item"]["liveChatSponsorshipsGiftRedemptionAnnouncementRenderer"];
                                            if (liveChatTextMessageRenderer is not null)
                                            {
                                                string tmpString = GetUserName(liveChatTextMessageRenderer) + GetChatText(liveChatTextMessageRenderer);

                                                // メンバーシップギフト受信のケース
                                                giftInfo.GiftType = "メンバーシップギフト受諾";
                                                giftInfo.GiftValue = 0;
                                                giftInfo.CommentText = tmpString;
                                                giftInfo.UserId = GetUserId(liveChatTextMessageRenderer);
                                                giftInfo.UserName = GetUserName(liveChatTextMessageRenderer);

                                                commentInfo.TimeStampUSec = GetChatTimestamp(liveChatTextMessageRenderer);

                                                userInfo.IconPath = GetUserIcon(liveChatTextMessageRenderer);
                                            }
                                            else
                                            {
                                                liveChatTextMessageRenderer = addChatItemAction["item"]["liveChatMembershipItemRenderer"];
                                                if (liveChatTextMessageRenderer is not null)
                                                {
                                                    // スパ茶とテキストのケース
                                                    (result, purchaseAmountText) = GetMembershipText(liveChatTextMessageRenderer);
                                                    // if (!result) { continue; }

                                                    giftInfo.GiftType = "メンバーシップチャット";
                                                    giftInfo.GiftValue = 0;
                                                    giftInfo.CommentText = $"{purchaseAmountText + "　" + GetChatText(liveChatTextMessageRenderer)}";
                                                    giftInfo.UserId = GetUserId(liveChatTextMessageRenderer);
                                                    giftInfo.UserName = GetUserName(liveChatTextMessageRenderer);

                                                    commentInfo.TimeStampUSec = GetChatTimestamp(liveChatTextMessageRenderer);

                                                    userInfo.IconPath = GetUserIcon(liveChatTextMessageRenderer);
                                                }
                                                else
                                                {
                                                    liveChatTextMessageRenderer = addChatItemAction["item"]["liveChatTickerSponsorItemRenderer"];
                                                    if (liveChatTextMessageRenderer is not null)
                                                    {
                                                        // メンバーシップ参加通知のケース
                                                        (result, var liveChatMembershipItemRenderer) = GetLiveChatMembershipItemRenderer(liveChatTextMessageRenderer);
                                                        if (liveChatMembershipItemRenderer is not null)
                                                        {
                                                            // スパ茶とテキストのケース
                                                            (result, purchaseAmountText) = GetMembershipText(liveChatMembershipItemRenderer);

                                                            giftInfo.GiftType = "メンバーシップ加入";
                                                            giftInfo.GiftValue = 0;
                                                            giftInfo.CommentText = purchaseAmountText;
                                                            giftInfo.UserId = GetUserId(liveChatTextMessageRenderer);
                                                            giftInfo.UserName = GetUserName(liveChatMembershipItemRenderer);

                                                            commentInfo.TimeStampUSec = GetChatTimestamp(liveChatMembershipItemRenderer);

                                                            userInfo.IconPath = GetUserIcon(liveChatMembershipItemRenderer);

                                                        }
                                                        else { continue; }
                                                    }
                                                    else { continue; }
                                                }
                                            }
                                        }
                                    }

                                    // スパ茶情報の取得
                                    giftInfo.TimeStampUSec = commentInfo.TimeStampUSec;
                                    giftInfo.StreamInfoId = this.StreamInfo.StreamId;
                                    giftInfo.CommentId = GetChatId(liveChatTextMessageRenderer);
                                    giftInfo.StreamManagementId = this.StreamManagementInfo.StreamManagementId;
                                    giftInfo.StreamSiteId = this.StreamInfo.StreamSiteId;

                                    // DBへデータ登録
                                    // await GiftApi.PostNewCommentToDb(this.StreamManagementInfo.StreamNo, giftInfo);
                                    giftInfoList.Add(giftInfo);

                                    // チャット情報の取得
                                    commentInfo.CommentText = giftInfo.CommentText;
                                    commentInfo.UserName = giftInfo.UserName;
                                }

                            }

                            // チャット情報の取得
                            commentInfo.StreamInfoId = this.StreamInfo.StreamId;
                            commentInfo.CommentId = GetChatId(liveChatTextMessageRenderer);
                            commentInfo.StreamManagementId = this.StreamManagementInfo.StreamManagementId;
                            commentInfo.StreamSiteId = this.StreamInfo.StreamSiteId;
                            commentInfo.UserId = GetUserId(liveChatTextMessageRenderer);

                            logger.Trace("[{0}]{1}:{2}", action.Index, commentInfo.UserName, commentInfo.CommentText);

                            // ユーザー情報の取得
                            userInfo.StreamSiteId = commentInfo.StreamSiteId;
                            userInfo.UserId = commentInfo.UserId;
                            userInfo.UserName = commentInfo.UserName;

                            // DB用オブジェクトへデータ登録
                            // ユーザー情報
                            // await UserApi.CreateOrUpdateUserInfo(userInfo);
                            userInfoList.Add(userInfo);

                            // コメント情報
                            // await CommentApi.PostNewCommentToDb(this.StreamManagementInfo.StreamNo, commentInfo);
                            commentInfoList.Add(commentInfo);

                            // イベントハンドラ呼び出し
                            if (this.GetCommentHandler != null)
                            {
                                var args = new GetNewCommentArgs();

                                args.StreamNo = this.StreamManagementInfo.StreamNo;
                                args.CommentId = commentInfo.CommentId;
                                args.TimeStampUSec = commentInfo.TimeStampUSec;
                                args.UserId = commentInfo.UserId;
                                args.UserName = commentInfo.UserName;
                                args.CommentText = commentInfo.CommentText;

                                this.GetCommentHandler(this, args);
                            }
                        }

                        // ユーザーをDBに登録
                        await this._commentApi.PostNewUserToDbByWebSocket(userInfoList);
                        // コメントをDBに登録
                        await this._commentApi.PostNewCommentToDbByWebSocket(this.StreamManagementInfo.StreamNo, commentInfoList);
                        // ギフトをDBに登録
                        await this._commentApi.PostNewGiftToDbByWebSocket(this.StreamManagementInfo.StreamNo, giftInfoList);

                        logger.Debug("チャット情報が取得できました。");
                    }
                    else
                    {
                        logger.Warn("チャット情報の取得に失敗した。");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "チャット取得でエラーが発生しました。エラーメッセージ:{0}", ex.Message);
                logger.Debug(ex.StackTrace);
            }

            logger.Debug("========== Func End!　 ==================================================");
        }

        /// <summary>
        /// コメントJsonData取得
        /// </summary>
        /// <param name="chatId"></param>
        /// <param name="liveId"></param>
        /// <returns></returns>
        private async Task<(int, JObject)> GetChatData(string chatId, string liveId)
        {
            int returnVal1 = -1; JObject returnVal2;
            string apiUrl = "https://www.youtube.com/youtubei/v1/live_chat/get_live_chat";
            logger.Debug("========== Func Start! ==================================================");

            try
            {
                string postJsonString =
                    @"{" +
                    @"""context"": {" +
                    @"""client"": {" +
                    @"""visitorData"": ""Cgsyc1p3Y3p2Z09mayjD6fOkBg%3D%3D""," +
                    @"""userAgent"": ""Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/114.0.0.0 Safari/537.36 Edg/114.0.1823.58,gzip(gfe)""," +
                    @"""clientName"": ""WEB""," +
                    @"""clientVersion"": ""2.20230628.01.00""," +
                    @"""osName"": ""Windows""," +
                    @"""osVersion"": ""10.0""," +
                    @"""originalUrl"": ""https://www.youtube.com/live_chat?is_popout=1&v=" + liveId + @"""," +
                    @"""screenPixelDensity"": 1," +
                    @"""platform"": ""DESKTOP""," +
                    @"""clientFormFactor"": ""UNKNOWN_FORM_FACTOR""," +
                    @"""configInfo"": {" +
                    @"""appInstallData"": ""CMPp86QGELiLrgUQ57qvBRD-7q4FEI-jrwUQ86ivBRCMt68FEOSz_hIQqcSvBRDqw68FEKHCrwUQ-r6vBRDn964FEOLUrgUQn7-vBRDDt_4SEP61rwUQs8mvBRCEtq8FEKLsrgUQpMuvBRCJ6K4FEOC2rwUQ7qKvBRDM364FENuvrwUQj8OvBRDdxq8FEMy3_hIQzK7-EhClma8FEKu3rwUQqrL-EhD4ta8FEKXC_hIQ1KGvBRDrk64FEL22rgUQ3ravBRC7tK8FEKvCrwUQn7mvBQ%3D%3D""" +
                    @"}," +
                    @"""screenDensityFloat"": 1.0," +
                    @"""timeZone"": ""Asia/Tokyo""," +
                    @"""browserName"": ""Edge Chromium""," +
                    @"""browserVersion"": ""114.0.1823.58""," +
                    @"""acceptHeader"": ""text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7""," +
                    @"}," +
                    @"""user"": {" +
                    @"""lockedSafetyMode"": false" +
                    @"}," +
                    @"""request"": {" +
                    @"""useSsl"": true ," +
                    @"""internalExperimentFlags"": [] ," +
                    @"""consistencyTokenJars"": [] " +
                    @"}" +
                    @"}," +
                    @"""continuation"": """ + chatId + @""", " +
                    @"""webClientInfo"": {" +
                    @"""isDocumentHidden"": false " +
                    @"}," +
                    @"""isInvalidationTimeoutRequest"": true " +
                    @"}";

                logger.Trace("post data:\r\n" + postJsonString);

                using (var client = new HttpClient())
                {
                    var content = new StringContent(postJsonString, Encoding.UTF8, "application/json");
                    var response = await client.PostAsync(apiUrl, content);

                    logger.Debug("status Code:" + response.StatusCode);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        JObject jsonObject = JObject.Parse(responseContent);
                        returnVal1 = 0;
                        returnVal2 = jsonObject;
                    }
                    else
                    {
                        logger.Error("チャットデータの取得に失敗しました。");
                        returnVal1 = -1;
                        returnVal2 = null;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "チャットデータの取得でエラーが発生しました。エラー:{0}", ex.Message);
                logger.Debug(ex.StackTrace);
                returnVal1 = -1;
                returnVal2 = null;
            }

            logger.Debug("========== Func End!　 ==================================================");
            return (returnVal1, returnVal2);
        }

        /// <summary>
        /// チャットIDの取得
        /// </summary>
        /// <param name="liveChatTextMessageRenderer"></param>
        /// <returns></returns>
        private string GetChatId(JToken liveChatTextMessageRenderer)
        {
            string returnVal = "";
            logger.Trace("========== Func Start! ==================================================");

            try
            {
                returnVal = liveChatTextMessageRenderer["id"].ToString();
            }
            catch (Exception ex)
            {
                logger.Error(ex, "チャットIDの取得でエラーが発生しました。エラーメッセージ:{0}", ex.Message);
                logger.Debug(ex.StackTrace);
                returnVal = "";
            }

            logger.Trace("========== Func End!　 ==================================================");
            return returnVal;
        }

        /// <summary>
        /// チャットタイムスタンプの取得
        /// </summary>
        /// <param name="liveChatTextMessageRenderer"></param>
        /// <returns></returns>
        private string GetChatTimestamp(JToken liveChatTextMessageRenderer)
        {
            string returnVal = "";
            logger.Trace("========== Func Start! ==================================================");

            try
            {
                returnVal = liveChatTextMessageRenderer["timestampUsec"].ToString();
            }
            catch (Exception ex)
            {
                logger.Error(ex, "チャットタイムスタンプの取得でエラーが発生しました。エラーメッセージ:{0}", ex.Message);
                logger.Debug(ex.StackTrace);
                returnVal = "";
            }

            logger.Trace("========== Func End!　 ==================================================");
            return returnVal;
        }

        /// <summary>
        /// チャットテキストの取得
        /// </summary>
        /// <param name="liveChatTextMessageRenderer"></param>
        /// <returns></returns>
        private string GetChatText(JToken liveChatTextMessageRenderer)
        {
            string returnVal = "";
            logger.Trace("========== Func Start! ==================================================");

            try
            {
                var message = liveChatTextMessageRenderer["message"];
                if (message is null)
                {
                    message = liveChatTextMessageRenderer["headerSubtext"];
                }
                if (message is not null)
                {
                    var runs = message["runs"];
                    foreach (var run in runs.Select((Value, Index) => (Value, Index)))
                    {
                        var text = run.Value["text"];
                        if (text is not null)
                        {
                            returnVal += text.ToString();
                        }
                        else
                        {
                            var emoji = run.Value["emoji"]["image"]["accessibility"]["accessibilityData"]["label"];
                            returnVal += emoji is not null ? "絵文字(" + emoji.ToString() + ")" : "※非対応情報※";
                        }
                    }
                }
                else
                {
                    message = liveChatTextMessageRenderer["purchaseAmountText"];
                    if (message is not null)
                    {
                        var text = message["simpleText"];
                        returnVal = $"スーパーチャット:{text.ToString()}";
                    }
                    else
                    {
                        returnVal = "";
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "チャットテキストの取得でエラーが発生しました。エラーメッセージ:{0}", ex.Message);
                logger.Debug(ex.StackTrace);
                returnVal = "";
            }

            logger.Trace("========== Func End!　 ==================================================");
            return returnVal;
        }

        /// <summary>
        /// スパ茶金額の取得
        /// </summary>
        /// <param name="liveChatPaidMessageRenderer"></param>
        /// <returns></returns>
        private (bool, string, int, string) GetPurchaseAmountText(JToken liveChatPaidMessageRenderer)
        {
            bool returnVal = false;
            string returnText = "", unitText = "";
            int purchaseValue = -1;
            logger.Trace("========== Func Start! ==================================================");

            try
            {
                string tmpString = ""; int tmpInt = -1;
                var simpleText = liveChatPaidMessageRenderer["purchaseAmountText"]["simpleText"];
                returnText = simpleText.ToString();

                // 単位と金額の分割
                if (returnText.IndexOf(@"￥") > -1)
                {
                    // 日本円
                    unitText = @"\";
                    tmpString = returnText.Replace(@"¥", "").Replace(",", "");
                    if (int.TryParse(tmpString, out tmpInt))
                    {
                        purchaseValue = tmpInt;
                    }
                    else
                    {
                        logger.Warn("金額のパースに失敗しました。");
                        purchaseValue = -1;
                    }
                }
                else if (returnText.IndexOf(@"$") > -1)
                {
                    // ドル系
                    if (returnText.IndexOf(@"NT$") > -1)
                    {
                        // ニュー台湾ドル
                        unitText = "台湾$";
                        tmpString = returnText.Replace("NT$", "");
                        if (int.TryParse(tmpString, out tmpInt))
                        {
                            purchaseValue = tmpInt;
                        }
                        else
                        {
                            logger.Warn("金額のパースに失敗しました。");
                            purchaseValue = -1;
                        }
                    }
                    else
                    {
                        // ドル
                        unitText = "US$";
                        tmpString = returnText.Replace("$", "");
                        if (int.TryParse(tmpString, out tmpInt))
                        {
                            purchaseValue = tmpInt;
                        }
                        else
                        {
                            logger.Warn("金額のパースに失敗しました。");
                            purchaseValue = -1;
                        }
                    }
                }
                else
                {
                    // 想定外通貨単位
                    logger.Warn("想定外の通貨単位です。");
                    purchaseValue = -1;
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "スパ茶金額テキストの取得でエラーが発生しました。エラーメッセージ:{0}", ex.Message);
                logger.Debug(ex.StackTrace);
                returnVal = false;
                returnText = "";
                purchaseValue = -1;
                unitText = "";
            }

            logger.Trace("========== Func End!　 ==================================================");
            return (returnVal, returnText, purchaseValue, unitText);
        }

        /// <summary>
        /// メンバーギフトメッセージ取得
        /// </summary>
        /// <param name="liveChatSponsorshipsGiftPurchaseAnnouncementRenderer"></param>
        /// <returns></returns>
        private (bool, string, int, string) GetMemberGifSendtText(JToken liveChatSponsorshipsHeaderRenderer)
        {
            bool returnVal = false;
            string returnText = "", unitText = "件";
            int purchaseValue = -1;
            logger.Trace("========== Func Start! ==================================================");

            try
            {
                var runs = liveChatSponsorshipsHeaderRenderer["primaryText"]["runs"];
                foreach (var run in runs.Select((Value, Index) => (Value, Index)))
                {
                    var text = run.Value["text"];
                    if (text is not null)
                    {
                        if (purchaseValue == -1)
                        {
                            int tmpInt = -1;
                            if (int.TryParse(text.ToString(), out tmpInt)) { purchaseValue = tmpInt; }
                        }
                        returnText += text.ToString();
                    }
                    else
                    {
                        var emoji = run.Value["emoji"]["image"]["accessibility"]["accessibilityData"]["label"];
                        returnText += emoji is not null ? "絵文字(" + emoji.ToString() + ")" : "※非対応情報※";
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "メンバーギフトメッセージの取得でエラーが発生しました。エラーメッセージ:{0}", ex.Message);
                logger.Debug(ex.StackTrace);
                returnVal = false;
                returnText = "";
            }

            logger.Trace("========== Func End!　 ==================================================");
            return (returnVal, returnText, purchaseValue, unitText);
        }

        /// <summary>
        /// メンバーシップテキストの取得
        /// </summary>
        /// <param name="liveChatMembershipItemRenderer"></param>
        /// <returns></returns>
        private (bool, string) GetMembershipText(JToken liveChatMembershipItemRenderer)
        {
            bool returnVal = false;
            string returnText = "";
            logger.Trace("========== Func Start! ==================================================");

            try
            {
                var text = liveChatMembershipItemRenderer["headerPrimaryText"];
                if (text is null)
                {
                    text = liveChatMembershipItemRenderer["headerSubtext"];
                }
                var runs = text["runs"];

                foreach (var run in runs.Select((Value, Index) => (Value, Index)))
                {
                    var tmpText = run.Value["text"];
                    if (text is not null)
                    {
                        returnText += tmpText.ToString();
                    }
                    else
                    {
                        var emoji = run.Value["emoji"]["image"]["accessibility"]["accessibilityData"]["label"];
                        returnText += emoji is not null ? "絵文字(" + emoji.ToString() + ")" : "※非対応情報※";
                    }
                }
                returnVal = true;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "メンバーシップテキストの取得でエラーが発生しました。エラーメッセージ:{0}", ex.Message);
                logger.Debug(ex.StackTrace);
                returnVal = false;
                returnText = "";
            }

            logger.Trace("========== Func End!　 ==================================================");
            return (returnVal, returnText);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="jToken"></param>
        /// <returns></returns>
        private (bool, JToken) GetLiveChatMembershipItemRenderer(JToken jToken)
        {
            bool returnVal = false;
            JToken returnJToken = "";
            logger.Trace("========== Func Start! ==================================================");

            try
            {
                returnJToken = jToken["showItemEndpoint"]["showLiveChatItemEndpoint"]["renderer"]["liveChatMembershipItemRenderer"];
            }
            catch (Exception ex)
            {
                logger.Error(ex, "liveChatMembershipItemRendererの取得でエラーが発生しました。エラーメッセージ:{0}", ex.Message);
                logger.Debug(ex.StackTrace);
                returnVal = false;
                returnJToken = null;
            }

            logger.Trace("========== Func End!　 ==================================================");
            return (returnVal, returnJToken);
        }

        /// <summary>
        /// ユーザーIDの取得
        /// </summary>
        /// <param name="liveChatTextMessageRenderer"></param>
        /// <returns></returns>
        private string GetUserId(JToken liveChatTextMessageRenderer)
        {
            string returnVal = "";
            logger.Trace("========== Func Start! ==================================================");

            try
            {
                returnVal = liveChatTextMessageRenderer["authorExternalChannelId"].ToString();
            }
            catch (Exception ex)
            {
                logger.Error(ex, "ユーザーIDの取得でエラーが発生しました。エラーメッセージ:{0}", ex.Message);
                logger.Debug(ex.StackTrace);
                returnVal = "";
            }

            logger.Trace("========== Func End!　 ==================================================");
            return returnVal;
        }

        /// <summary>
        /// ユーザーネームの取得
        /// </summary>
        /// <param name="liveChatTextMessageRenderer"></param>
        /// <returns></returns>
        private string GetUserName(JToken liveChatTextMessageRenderer)
        {
            string returnVal = "";
            logger.Trace("========== Func Start! ==================================================");

            try
            {
                returnVal = liveChatTextMessageRenderer["authorName"]["simpleText"].ToString();
            }
            catch (Exception ex)
            {
                logger.Error(ex, "ユーザー名の取得でエラーが発生しました。エラーメッセージ:{0}", ex.Message);
                logger.Debug(ex.StackTrace);
                returnVal = "";
            }

            logger.Trace("========== Func End!　 ==================================================");
            return returnVal;
        }

        /// <summary>
        /// ユーザーアイコンの取得
        /// </summary>
        /// <param name="liveChatTextMessageRenderer"></param>
        /// <returns></returns>
        private string GetUserIcon(JToken liveChatTextMessageRenderer)
        {
            string returnVal = "";
            logger.Trace("========== Func Start! ==================================================");

            try
            {
                var thumbnails = liveChatTextMessageRenderer["authorPhoto"]["thumbnails"];
                foreach (var thumbnail in thumbnails.Select((Value, Index) => (Value, Index)))
                {
                    var url = thumbnail.Value["url"];
                    if (string.IsNullOrWhiteSpace(returnVal))
                    {
                        returnVal = url.ToString();
                    }
                    else
                    {
                        returnVal += "<br>" + url.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "ユーザーアイコンの取得でエラーが発生しました。エラーメッセージ:{0}", ex.Message);
                logger.Debug(ex.StackTrace);
                returnVal = "";
            }

            logger.Trace("========== Func End!　 ==================================================");
            return returnVal;
        }

        /// <summary>
        /// デスコンストラクター
        /// </summary>
        public void Dispose()
        {
            logger.Trace("========== Func Start! ==================================================");

            this._cancellationTokenSource.Cancel();
            if (this._task is not null)
            {
                while (!this._task.IsCompleted)
                {
                    this._task.WaitAsync(new TimeSpan(0, 0, 0, 30));
                }
                this._task.Dispose();
            }
            this._commentApi.Dispose();
            this._cancellationTokenSource.Dispose();

            logger.Trace("========== Func End!　 ==================================================");
        }
    }
}