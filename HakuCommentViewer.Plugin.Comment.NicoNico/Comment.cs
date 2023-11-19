using System;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Newtonsoft.Json.Linq;

using HakuCommentViewer.Common.Controllers;
using HakuCommentViewer.Common.Models;
using HakuCommentViewer.Plugin;
using IPlugin = HakuCommentViewer.Plugin.Interface;
using HakuCommentViewer.Plugin.Models;

namespace HakuCommentViewer.Plugin.Comment.NicoNico
{
    /// <summary>
    /// ニコ生コメント処理クラス
    /// </summary>
    public class Comment : IPlugin.IComment
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

        /// <summary>
        /// API URL
        /// </summary>
        private string websocketUrl = "";

        /// <summary>
        /// コメント投稿用スレッドKey
        /// </summary>
        private string threadKey = "";
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
        /// 受信受信イベントハンドラー
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
            this.PluginInfo.Id = "F342E7F0-F93A-434A-9890-C87F1A197F80";
            this.PluginInfo.Name = "コメント取得/送信プラグイン：ニコニコ生放送";
            this.PluginInfo.Author = "風丸　白";
            this.PluginInfo.Description = "ニコニコ生放送のコメントを取得/送信するプラグインです。※Comment送信機能は未実装。";
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
            logger.Debug("========== Func Start! ==================================================");

            try
            {
                // JSON取得
                JObject jObject = await GetStreamInfo(this.StreamInfo.StreamUrl);

                // StreamID抽出
                this.StreamInfo.StreamPageId = GetStreamId(jObject);
                // 配信名取得
                this.StreamInfo.StreamName = GetStreamTitle(jObject);
                // 配信開始日時取得
                this.StreamInfo.StartDateTime = GetStreamStartDateTime(jObject);
                // 配信終了取得
                this.StreamInfo.EndDateTime = GetStreamEndDateTime(jObject);
                // 視聴者数
                this.StreamInfo.ViewerCount = GetStreamViewerCount(jObject);
                // コメント数
                this.StreamInfo.CommentCount = GetStreamCommentCount(jObject);
                // 配信ステータス取得
                this.StreamInfo.LiveStatus = GetLiveStatus(jObject);

                if (this.StreamInfo.LiveStatus > 0)
                {
                    // API URL取得
                    this.websocketUrl = GetWebsocketUrl(jObject);

                    // チャットスレッドID取得
                    if (!string.IsNullOrEmpty(this.websocketUrl))
                    {
                        bool waitLoopFlag = true;
                        using (CancellationTokenSource cancellationTokenSource = new CancellationTokenSource())
                        using (var task = Task.Run(() => GetStreamInfoByWebSocket(this.websocketUrl, cancellationTokenSource.Token), cancellationToken))
                        {
                            while (waitLoopFlag)
                            {
                                if (cancellationToken.IsCancellationRequested)
                                {
                                    logger.Info("処理キャンセル信号を受信しました。");
                                    cancellationTokenSource.Cancel();
                                    task.Wait();
                                    waitLoopFlag = false;
                                }

                                if (this.StreamInfo.CommentRoomId != "%NULL%")
                                {
                                    cancellationTokenSource.Cancel();
                                    task.Wait();
                                    waitLoopFlag = false;
                                }

                                Thread.Sleep(250);
                            }
                        }
                    }
                    else
                    {
                        logger.Warn("WebSocket API URLの取得に失敗しました。");
                    }
                }
                returnVal = true;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "配信情報取得でエラーが発生しました。エラーメッセージ:{0}", ex.Message);
                logger.Debug(ex.StackTrace);
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

            // 配信情報取得
            returnVal = await GetStreamInfo(cancellationToken);

            // チャットスレッドID取得
            if (returnVal)
            {
                if (this.StreamInfo.LiveStatus > 0 && !string.IsNullOrEmpty(this.websocketUrl))
                {
                    // コメント取得開始
                    this._task = this.CommentGetLoop(cancellationToken);
                    returnVal = true;
                }
                else if (this.StreamInfo.LiveStatus <= 0)
                {
                    logger.Error("配信枠が配信状態ではありません。");
                    returnVal = false;
                }
                else
                {
                    logger.Error("WebSocket API URLの取得に失敗しました。");
                    returnVal = false;
                }
            }
            else
            {
                logger.Error("配信情報の取得に失敗しました。");
                returnVal = false;
            }

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
        /// 配信枠情報JSON取得
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private async Task<JObject> GetStreamInfo(string url)
        {
            JObject returnVal = null;
            logger.Debug("========== Func Start! ==================================================");

            try
            {
                var responseData = default(IHtmlDocument);

                logger.Debug("取得先URL：" + url);

                // スクレイピング
                using (var client = new HttpClient())
                using (var stream = await client.GetStreamAsync(new Uri(url)))
                {
                    var parser = new HtmlParser();
                    responseData = await parser.ParseDocumentAsync(stream);
                }

                logger.Debug("ページタイトル：" + responseData.Title);

                string scriptData = responseData.GetElementById("embedded-data").Attributes["data-props"].Value;
                // logger.Trace("json:\n" + jsonString);
                returnVal = JObject.Parse(scriptData);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "配信枠情報JSONの取得でエラーが発生しました。エラーメッセージ:{0}", ex.Message);
                logger.Debug(ex.StackTrace);
                returnVal = null;
            }

            logger.Debug("========== Func End!　 ==================================================");
            return returnVal;
        }

        /// <summary>
        /// 配信ID取得
        /// </summary>
        /// <param name="jsonObject"></param>
        /// <returns></returns>
        private string GetStreamId(JObject jsonObject)
        {
            string returnVal = "";
            logger.Trace("========== Func Start! ==================================================");

            try
            {
                returnVal = jsonObject["program"]["nicoliveProgramId"].ToString();
            }
            catch (Exception ex)
            {
                logger.Error(ex, "配信IDの取得でエラーが発生しました。エラーメッセージ:{0}", ex.Message);
                logger.Debug(ex.StackTrace);
                returnVal = "";
            }

            logger.Trace("========== Func End!　 ==================================================");
            return returnVal;
        }

        /// <summary>
        /// 配信名取得
        /// </summary>
        /// <param name="jsonObject"></param>
        /// <returns></returns>
        private string GetStreamTitle(JObject jsonObject)
        {
            string returnVal = "";
            logger.Trace("========== Func Start! ==================================================");

            try
            {
                returnVal = jsonObject["program"]["title"].ToString();
            }
            catch (Exception ex)
            {
                logger.Error(ex, "配信名の取得でエラーが発生しました。エラーメッセージ:{0}", ex.Message);
                logger.Debug(ex.StackTrace);
                returnVal = "";
            }

            logger.Trace("========== Func End!　 ==================================================");
            return returnVal;
        }

        /// <summary>
        /// 配信開始日時取得
        /// </summary>
        /// <param name="jsonObject"></param>
        /// <returns></returns>
        private DateTime GetStreamStartDateTime(JObject jsonObject)
        {
            DateTime returnVal = DateTime.MinValue;
            logger.Trace("========== Func Start! ==================================================");

            try
            {
                string tmpString = ""; int tmpValue = 0;
                tmpString = jsonObject["program"]["beginTime"] is not null ?
                    jsonObject["program"]["beginTime"].ToString() :
                    jsonObject["program"]["openTime"].ToString();
                if (int.TryParse(tmpString, out tmpValue))
                {
                    returnVal = DateTimeOffset.FromUnixTimeSeconds(tmpValue).ToLocalTime().DateTime;
                }
                else
                {
                    returnVal = DateTime.MinValue;
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "配信開始日時の取得でエラーが発生しました。エラーメッセージ:{0}", ex.Message);
                logger.Debug(ex.StackTrace);
                returnVal = DateTime.MinValue;
            }

            logger.Trace("========== Func End!　 ==================================================");
            return returnVal;
        }

        /// <summary>
        /// 配信終了日時取得
        /// </summary>
        /// <param name="jsonObject"></param>
        /// <returns></returns>
        private DateTime GetStreamEndDateTime(JObject jsonObject)
        {
            DateTime returnVal = DateTime.MinValue;
            logger.Trace("========== Func Start! ==================================================");

            try
            {
                string tmpString = ""; int tmpValue = 0;
                tmpString = jsonObject["program"]["endTime"] is not null ?
                    jsonObject["program"]["endTime"].ToString() :
                    jsonObject["program"]["scheduledEndTime"].ToString();
                if (int.TryParse(tmpString, out tmpValue))
                {
                    returnVal = DateTimeOffset.FromUnixTimeSeconds(tmpValue).ToLocalTime().DateTime;
                }
                else
                {
                    returnVal = DateTime.MinValue;
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "配信終了日時の取得でエラーが発生しました。エラーメッセージ:{0}", ex.Message);
                logger.Debug(ex.StackTrace);
                returnVal = DateTime.MinValue;
            }

            logger.Trace("========== Func End!　 ==================================================");
            return returnVal;
        }

        /// <summary>
        /// 視聴者数取得
        /// </summary>
        /// <param name="jsonObject"></param>
        /// <returns></returns>
        private int GetStreamViewerCount(JObject jsonObject)
        {
            int returnVal = -1;
            logger.Trace("========== Func Start! ==================================================");

            try
            {
                string tmpString = ""; int tmpValue = 0;
                tmpString = jsonObject["program"]["statistics"]["watchCount"].ToString();
                if (int.TryParse(tmpString, out tmpValue))
                {
                    returnVal = tmpValue;
                }
                else
                {
                    returnVal = -1;
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "視聴者数の取得でエラーが発生しました。エラーメッセージ:{0}", ex.Message);
                logger.Debug(ex.StackTrace);
                returnVal = -1;
            }

            logger.Trace("========== Func End!　 ==================================================");
            return returnVal;
        }

        /// <summary>
        /// コメント数取得
        /// </summary>
        /// <param name="jsonObject"></param>
        /// <returns></returns>
        private int GetStreamCommentCount(JObject jsonObject)
        {
            int returnVal = -1;
            logger.Trace("========== Func Start! ==================================================");

            try
            {
                string tmpString = ""; int tmpValue = 0;
                tmpString = jsonObject["program"]["statistics"]["commentCount"].ToString();
                if (int.TryParse(tmpString, out tmpValue))
                {
                    returnVal = tmpValue;
                }
                else
                {
                    returnVal = -1;
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "コメント数の取得でエラーが発生しました。エラーメッセージ:{0}", ex.Message);
                logger.Debug(ex.StackTrace);
                returnVal = -1;
            }

            logger.Trace("========== Func End!　 ==================================================");
            return returnVal;
        }

        /// <summary>
        /// 配信ステータス取得
        /// </summary>
        /// <param name="jsonObject"></param>
        /// <returns></returns>
        private int GetLiveStatus(JObject jsonObject)
        {
            int returnVal = -1;
            logger.Trace("========== Func Start! ==================================================");

            try
            {
                string tmpString = jsonObject["program"]["status"].ToString();
                switch (tmpString)
                {
                    case "RELEASED":
                        {
                            returnVal = 0;
                            break;
                        }
                    case "ON_AIR":
                        {
                            returnVal = 1;
                            break;
                        }
                    case "ENDED":
                        {
                            returnVal = 0;
                            break;
                        }
                    default:
                        {
                            returnVal = -1;
                            break;
                        }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "配信ステータスの取得でエラーが発生しました。エラーメッセージ:{0}", ex.Message);
                logger.Debug(ex.StackTrace);
                returnVal = -1;
            }

            logger.Trace("========== Func End!　 ==================================================");
            return returnVal;
        }

        /// <summary>
        /// ChatAPI URL取得メソッド
        /// </summary>
        /// <param name="jsonObject"></param>
        /// <returns></returns>
        private string GetWebsocketUrl(JObject jsonObject)
        {
            string returnVal = "";
            logger.Trace("========== Func Start! ==================================================");

            try
            {
                string wesocketUrl = jsonObject["site"]["relive"]["webSocketUrl"].ToString();
                returnVal = wesocketUrl;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "ChatAPI URLの取得でエラーが発生しました。エラーメッセージ:{0}", ex.Message);
                logger.Debug(ex.StackTrace);
                returnVal = "";
            }

            logger.Trace("========== Func End!　 ==================================================");
            return returnVal;
        }

        /// <summary>
        /// 配信情報の取得を開始
        /// </summary>
        /// <param name="websocketUrl"></param>
        /// <returns></returns>
        private async Task<int> GetStreamInfoByWebSocket(string websocketUrl, CancellationToken cancellationToken)
        {
            int returnVal = -1;
            logger.Debug("========== Func Start! ==================================================");

            try
            {
                logger.Debug("取得先URL：" + websocketUrl);

                using (var client = new ClientWebSocket())
                {
                    Stopwatch stopwatch = Stopwatch.StartNew();
                    int opened = 0, keepIntervalSec = 30;

                    // client.Options.SetRequestHeader("Sec-WebSocket-Protocol", "msg.nicovideo.jp#json");
                    client.Options.SetRequestHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/114.0.0.0 Safari/537.36 Edg/114.0.1823.67");
                    await client.ConnectAsync(new Uri(websocketUrl), CancellationToken.None);

                    while (client.State == WebSocketState.Open || client.State == WebSocketState.Connecting)
                    {
                        var buffer = new byte[1024];
                        //所得情報確保用の配列を準備
                        var segment = new ArraySegment<byte>(buffer);
                        //サーバからのレスポンス情報を取得
                        var result = await client.ReceiveAsync(segment, CancellationToken.None);

                        //エンドポイントCloseの場合、処理を中断
                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "OK", CancellationToken.None);
                            logger.Info("サーバーから切断されました。");
                            break;
                        }

                        //バイナリの場合は、当処理では扱えないため、処理を中断
                        if (result.MessageType == WebSocketMessageType.Binary)
                        {
                            await client.CloseAsync(WebSocketCloseStatus.InvalidMessageType, "I don't do binary", CancellationToken.None);
                            logger.Error("サーバーからバイナリーデータが返却されました。");
                            break;
                        }

                        // 接続開始処理（一度だけ実行）
                        switch (opened)
                        {
                            case 0:
                                {
                                    string sendString = @"{""type"":""startWatching"",""data"":{""stream"":{""quality"":""abr"",""protocol"":""hls+fmp4"",""latency"":""low"",""chasePlay"":false},""room"":{""protocol"":""webSocket"",""commentable"":true},""reconnect"":false}}";
                                    var tmpBuffer = Encoding.UTF8.GetBytes(sendString);
                                    var tmpSegment = new ArraySegment<byte>(tmpBuffer);

                                    await client.SendAsync(tmpSegment, WebSocketMessageType.Text, true, CancellationToken.None);
                                    logger.Debug("post>" + sendString);

                                    opened = 1;
                                    break;
                                }
                            case 1:
                                {
                                    string sendString = @"{""type"":""getAkashic"",""data"":{""chasePlay"":false}}";
                                    var tmpBuffer = Encoding.UTF8.GetBytes(sendString);
                                    var tmpSegment = new ArraySegment<byte>(tmpBuffer);

                                    await client.SendAsync(tmpSegment, WebSocketMessageType.Text, true, CancellationToken.None);
                                    logger.Debug("post>" + sendString);

                                    opened = 2;
                                    break;
                                }
                            default:
                                // キープアライブ
                                {
                                    if (stopwatch.ElapsedMilliseconds > (keepIntervalSec * 1000))
                                    {
                                        string sendString = @"{""type"":""keepSeat""}";
                                        var tmpBuffer = Encoding.UTF8.GetBytes(sendString);
                                        var tmpSegment = new ArraySegment<byte>(tmpBuffer);

                                        await client.SendAsync(tmpSegment, WebSocketMessageType.Text, true, CancellationToken.None);
                                        logger.Trace("post>" + sendString);

                                        stopwatch.Restart();
                                    }
                                    break;
                                }
                        }

                        //メッセージの最後まで取得
                        int count = result.Count;
                        while (!result.EndOfMessage)
                        {
                            if (count >= buffer.Length)
                            {
                                await client.CloseAsync(WebSocketCloseStatus.InvalidPayloadData, "That's too long", CancellationToken.None);
                                logger.Warn("サーバーから返却されたデータがバッファーを超えました。");
                                break;
                            }
                            segment = new ArraySegment<byte>(buffer, count, buffer.Length - count);
                            result = await client.ReceiveAsync(segment, CancellationToken.None);

                            count += result.Count;
                        }

                        //メッセージを取得
                        var message = Encoding.UTF8.GetString(buffer, 0, count);
                        JObject responseJson = JObject.Parse(message);

                        switch (responseJson["type"].ToString())
                        {
                            case "ping":
                                // キープアライブ
                                {
                                    string sendString = @"{""type"":""pong""}";
                                    logger.Trace("response> Ping!");
                                    var tmpBuffer = Encoding.UTF8.GetBytes(sendString);
                                    var tmpSegment = new ArraySegment<byte>(tmpBuffer);

                                    await client.SendAsync(tmpSegment, WebSocketMessageType.Text, true, CancellationToken.None);
                                    logger.Trace("post>" + sendString);

                                    break;
                                }
                            case "statistics":
                                // 視聴者数、コメント数、累計広告ポイント、累計ギフトポイント
                                {
                                    int tmpInt = 0;
                                    logger.Trace("response> Statistics info :" + responseJson["data"].ToString());
                                    if (int.TryParse(responseJson["data"]["comments"].ToString(), out tmpInt))
                                    {
                                        this.StreamInfo.CommentCount = tmpInt;
                                    }

                                    break;
                                }
                            case "akashic":
                                {
                                    logger.Trace("response> Akashic info :" + responseJson["data"].ToString());
                                    break;
                                }
                            case "room":
                                // チャットスレッドID、投稿キー
                                {
                                    logger.Trace("response> Room info :" + responseJson["data"].ToString());
                                    this.StreamInfo.CommentRoomId = responseJson["data"]["threadId"].ToString();
                                    if (((JObject)responseJson["data"]).ContainsKey("yourPostKey"))
                                    {
                                        this.threadKey = responseJson["data"]["yourPostKey"].ToString();
                                    }
                                    break;
                                }
                            case "schedule":
                                // 配信開始日時、配信終了予定日時
                                {
                                    DateTime dateTime;
                                    logger.Trace("response> Schedule info :" + responseJson["data"].ToString());

                                    if (((JObject)responseJson["data"]).ContainsKey("begin"))
                                    {
                                        if (DateTime.TryParse(responseJson["data"]["begin"].ToString(), out dateTime))
                                        {
                                            this.StreamInfo.StartDateTime = dateTime;
                                        }
                                    }
                                    if (((JObject)responseJson["data"]).ContainsKey("end"))
                                    {
                                        if (DateTime.TryParse(responseJson["data"]["end"].ToString(), out dateTime))
                                        {
                                            this.StreamInfo.EndDateTime = dateTime;
                                        }
                                    }

                                    break;
                                }
                            case "stream":
                                // ストリームURL
                                {
                                    logger.Trace("response> Stream info :" + responseJson["data"].ToString());
                                    break;
                                }
                            case "seat":
                                // 座席情報（キープアライブ間隔）
                                {
                                    int tmpInt = 0;
                                    logger.Trace("response> Seat keep interval sec :" + responseJson["data"]["keepIntervalSec"].ToString());

                                    JToken jToken = responseJson.SelectToken("data.keepIntervalSec");
                                    if (jToken is not null)
                                    {
                                        if (int.TryParse(jToken.ToString(), out tmpInt))
                                        {
                                            keepIntervalSec = tmpInt;
                                        }
                                    }

                                    break;
                                }
                            case "serverTime":
                                // サーバー時間
                                {
                                    logger.Trace("response> Sever Time :" + responseJson["data"]["currentMs"].ToString());
                                    break;
                                }
                            default:
                                {
                                    logger.Trace("response> " + message);
                                    break;
                                }
                        }

                        if (cancellationToken.IsCancellationRequested)
                        {
                            await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "OK", CancellationToken.None);
                            logger.Info("サーバーから切断しました。");
                            break;
                        }
                    }
                }
                returnVal = 0;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "配信情報の取得でエラーが発生しました。", ex.Message);
                logger.Debug(ex.StackTrace);
                returnVal = -1;
            }

            logger.Debug("========== Func End!　 ==================================================");
            return returnVal;
        }

        /// <summary>
        /// コメント取得メインループ
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task CommentGetLoop(CancellationToken cancellationToken)
        {
            logger.Debug("========== Func Start! ==================================================");

            try
            {
                if (!string.IsNullOrEmpty(websocketUrl))
                {
                    if (!string.IsNullOrWhiteSpace(this.StreamInfo.CommentRoomId))
                    {
                        logger.Info("チャットチャンネルID：" + this.StreamInfo.CommentRoomId);
                        logger.Info("チャット投稿キー　　：" + this.threadKey);
                        int result = await GetChatData(this.StreamInfo.CommentRoomId, this.threadKey, cancellationToken);
                    }
                    else
                    {
                        logger.Error("チャット情報が取得できませんでした。");
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
        /// ニコ生コメント・配信情報取得処理
        /// </summary>
        /// <param name="threadId"></param>
        /// <param name="threadKey"></param>
        /// <returns></returns>
        private async Task<int> GetChatData(string threadId, string threadKey, CancellationToken cancellationToken)
        {
            int returnVal = -1;
            const string websocketUrl = "wss://msgd.live2.nicovideo.jp/websocket";
            logger.Debug("========== Func Start! ==================================================");

            try
            {
                logger.Debug("取得先URL：" + websocketUrl);

                using (var client = new ClientWebSocket())
                {
                    Stopwatch stopwatch = Stopwatch.StartNew();
                    int opened = 0, keepIntervalSec = 60;

                    client.Options.SetRequestHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/114.0.0.0 Safari/537.36 Edg/114.0.1823.67");
                    client.Options.AddSubProtocol("msg.nicovideo.jp#json");
                    await client.ConnectAsync(new Uri(websocketUrl), CancellationToken.None);

                    while (client.State == WebSocketState.Open || client.State == WebSocketState.Connecting)
                    {
                        var buffer = new byte[1024];
                        //所得情報確保用の配列を準備
                        var segment = new ArraySegment<byte>(buffer);

                        // 接続開始処理（一度だけ実行）
                        switch (opened)
                        {
                            case 0:
                                {
                                    string sendString;
                                    if (!string.IsNullOrWhiteSpace(threadKey))
                                    {
                                        // 投稿キーあり
                                        sendString = @"
[
    {
        ""ping"": {
            ""content"": ""rs:0""
        }
    },
    {
        ""ping"": {
            ""content"": ""ps:0""
        }
    },
    {
        ""thread"": {
            ""thread"": ""@threadId"",
            ""version"": ""20061206"",
            ""res_from"": -250,
            ""with_global"": 1,
            ""scores"": 1,
            ""nicoru"": 0,
            ""threadkey"": ""@threadKey""
        }
    },
    {
        ""ping"": {
            ""content"": ""pf:0""
        }
    },
    {
        ""ping"": {
            ""content"": ""rf:0""
        }
    }
]";

                                    }
                                    else
                                    {
                                        // 投稿キーなし
                                        sendString = @"
[
    {
        ""ping"": {
            ""content"": ""rs:0""
        }
    },
    {
        ""ping"": {
            ""content"": ""ps:0""
        }
    },
    {
        ""thread"": {
            ""thread"": ""@threadId"",
            ""version"": ""20061206"",
            ""res_from"": -250,
            ""with_global"": 1,
            ""scores"": 1,
            ""nicoru"": 0
        }
    },
    {
        ""ping"": {
            ""content"": ""pf:0""
        }
    },
    {
        ""ping"": {
            ""content"": ""rf:0""
        }
    }
]";
                                    }

                                    sendString = sendString.Replace("@threadId", threadId).Replace("@threadKey", threadKey);

                                    var tmpBuffer = Encoding.UTF8.GetBytes(sendString);
                                    var tmpSegment = new ArraySegment<byte>(tmpBuffer);

                                    await client.SendAsync(tmpSegment, WebSocketMessageType.Text, true, CancellationToken.None);
                                    logger.Debug("post>" + sendString);

                                    opened = 1;
                                    break;
                                }
                            default:
                                {
                                    // キープアライブ処理
                                    if (stopwatch.ElapsedMilliseconds > (keepIntervalSec * 1000))
                                    {
                                        string sendString = "";
                                        var tmpBuffer = Encoding.UTF8.GetBytes(sendString);
                                        var tmpSegment = new ArraySegment<byte>(tmpBuffer);

                                        await client.SendAsync(tmpSegment, WebSocketMessageType.Text, true, CancellationToken.None);
                                        logger.Debug("post> keepAlive");
                                        stopwatch.Restart();
                                    }
                                    break;
                                }
                        }

                        //サーバからのレスポンス情報を取得
                        var result = await client.ReceiveAsync(segment, CancellationToken.None);

                        //エンドポイントCloseの場合、処理を中断
                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "OK", CancellationToken.None);
                            logger.Debug("サーバーから切断されました。");
                            break;
                        }

                        //バイナリの場合は、当処理では扱えないため、処理を中断
                        if (result.MessageType == WebSocketMessageType.Binary)
                        {
                            await client.CloseAsync(WebSocketCloseStatus.InvalidMessageType, "I don't do binary", CancellationToken.None);
                            logger.Error("サーバーからバイナリーデータが返却されました。");
                            break;
                        }

                        //メッセージの最後まで取得
                        int count = result.Count;
                        while (!result.EndOfMessage)
                        {
                            if (count >= buffer.Length)
                            {
                                await client.CloseAsync(WebSocketCloseStatus.InvalidPayloadData, "That's too long", CancellationToken.None);
                                logger.Warn("サーバーから返却されたデータがバッファーを超えました。");
                                break;
                            }
                            segment = new ArraySegment<byte>(buffer, count, buffer.Length - count);
                            result = await client.ReceiveAsync(segment, CancellationToken.None);

                            count += result.Count;
                        }

                        //メッセージを取得
                        var message = Encoding.UTF8.GetString(buffer, 0, count);
                        JObject responseJson = JObject.Parse(message);

                        if (responseJson.ContainsKey("ping"))
                        {
                            // キープアライブ
                            string sendString = @"{""type"":""pong""}";
                            logger.Debug("response> Ping!");
                            var tmpBuffer = Encoding.UTF8.GetBytes(sendString);
                            var tmpSegment = new ArraySegment<byte>(tmpBuffer);

                            await client.SendAsync(tmpSegment, WebSocketMessageType.Text, true, CancellationToken.None);
                            logger.Debug("post>" + sendString);

                            sendString = @"{""type"":""keepSeat""}";
                            logger.Debug("response> Ping!");
                            tmpBuffer = Encoding.UTF8.GetBytes(sendString);
                            tmpSegment = new ArraySegment<byte>(tmpBuffer);

                            await client.SendAsync(tmpSegment, WebSocketMessageType.Text, true, CancellationToken.None);
                            logger.Debug("post>" + sendString);
                        }
                        else if (responseJson.ContainsKey("chat"))
                        {
                            // コメント取得時
                            bool hiddenname = false;
                            CommentInfo commentInfo = new CommentInfo();
                            UserInfo userInfo = new UserInfo();

                            logger.Debug("response> Chat info :" + responseJson["chat"].ToString());

                            // コメント情報の取得
                            commentInfo.StreamInfoId = this.StreamInfo.StreamId;
                            commentInfo.TimeStampUSec = responseJson["chat"]["date"].ToString() + responseJson["chat"]["date_usec"].ToString();
                            if (((JObject)responseJson["chat"]).ContainsKey("no"))
                            {
                                commentInfo.CommentId = responseJson["chat"]["no"].ToString();
                            }
                            else if (((JObject)responseJson["chat"]).ContainsKey("vpos"))
                            {
                                commentInfo.CommentId = responseJson["chat"]["vpos"].ToString();
                            }
                            else
                            {
                                commentInfo.CommentId = commentInfo.TimeStampUSec;
                            }
                            commentInfo.StreamManagementId = this.StreamManagementInfo.StreamManagementId;
                            commentInfo.StreamSiteId = this.StreamInfo.StreamSiteId;
                            commentInfo.UserId = responseJson["chat"]["user_id"].ToString();
                            if (((JObject)responseJson["chat"]).ContainsKey("premium"))
                            {
                                if (responseJson["chat"]["premium"].ToString() == "3")
                                {
                                    if (responseJson["chat"]["name"] is not null)
                                    {
                                        commentInfo.UserName = responseJson["chat"]["name"].ToString();
                                    }
                                    else
                                    {
                                        if (responseJson["chat"]["yourpost"] is not null)
                                        {
                                            commentInfo.UserName = "自コメ";
                                        }
                                        else
                                        {
                                            commentInfo.UserName = "システムメッセージ:ニコニコ生放送";
                                        }
                                    }
                                }
                                else
                                {
                                    if (responseJson["chat"]["name"] is not null)
                                    {
                                        commentInfo.UserName = responseJson["chat"]["name"].ToString();
                                    }
                                    else
                                    {
                                        if (responseJson["chat"]["yourpost"] is not null)
                                        {
                                            commentInfo.UserName = "自コメ";
                                        }
                                        else
                                        {
                                            commentInfo.UserName = "名無しさん(184)";
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (responseJson["chat"]["name"] is not null)
                                {
                                    commentInfo.UserName = responseJson["chat"]["name"].ToString();
                                }
                                else
                                {
                                    if (responseJson["chat"]["yourpost"] is not null)
                                    {
                                        commentInfo.UserName = "自コメ";
                                    }
                                    else
                                    {
                                        commentInfo.UserName = "名無しさん(184)";
                                    }
                                }
                            }
                            commentInfo.CommentText = responseJson["chat"]["content"].ToString();

                            // ニコニ広告
                            if (commentInfo.CommentText.IndexOf("/nicoad") > -1)
                            {
                                GiftInfo giftInfo = new GiftInfo();
                                bool error = false;
                                int nicoadPoint = -1;
                                string tmpString, nicoadUserId = "", nicoadUserName = "";
                                string jsonString = commentInfo.CommentText.Replace("/nicoad ", "");
                                JObject nicoadJobject = JObject.Parse(jsonString);
                                string nicoadMessage = nicoadJobject["message"].ToString();

                                // 広告貢献ありパターン
                                Match match = Regex.Match(nicoadMessage, "(【広告貢献[0-9]位】)(.+さんが)([0-9]+pt)(ニコニ広告しました)");

                                if (match.Success == true)
                                {
                                    // 広告ポイント
                                    tmpString = match.Groups[3].Value;
                                    if (!int.TryParse(tmpString.Replace("pt", ""), out nicoadPoint)) { nicoadPoint = -1; }
                                    // 広告者名
                                    nicoadUserName = match.Groups[2].Value;
                                    nicoadUserName = nicoadUserName.Substring(0, nicoadUserName.Length - 3);
                                }
                                else
                                {
                                    match = Regex.Match(nicoadMessage, "(.+さんが)([0-9]+pt)(ニコニ広告しました)");
                                    if (match.Success == true)
                                    {
                                        // 広告ポイント
                                        tmpString = match.Groups[2].Value;
                                        if (!int.TryParse(tmpString.Replace("pt", ""), out nicoadPoint)) { nicoadPoint = -1; }
                                        // 広告者名
                                        nicoadUserName = match.Groups[1].Value;
                                        nicoadUserName = nicoadUserName.Substring(0, nicoadUserName.Length - 3);
                                    }
                                    else
                                    {
                                        error = true;
                                    }
                                }

                                if (!error)
                                {
                                    // ユーザーID検索
                                    UserInfo tmpUserInfo = new UserInfo();

                                    tmpUserInfo.StreamSiteId = commentInfo.StreamSiteId;
                                    tmpUserInfo.UserName = nicoadUserName;
                                    tmpUserInfo = await UserApi.GetUserInfoFromDbByName(tmpUserInfo);

                                    if (tmpUserInfo is not null) { nicoadUserId = tmpUserInfo.UserId; }
                                    else
                                    {
                                        tmpUserInfo = new UserInfo();
                                        tmpUserInfo.StreamSiteId = commentInfo.StreamSiteId;
                                        tmpUserInfo.UserId = Guid.NewGuid().ToString();
                                        tmpUserInfo.UserName = nicoadUserName;
                                        tmpUserInfo.IconPath = "https://secure-dcdn.cdn.nimg.jp/nicoaccount/usericon/defaults/blank_s.jpg";
                                        tmpUserInfo.Note = "ニコニ広告　広告者";
                                        // await UserApi.CreateOrUpdateUserInfo(tmpUserInfo);
                                        await this._commentApi.PostNewUserToDbByWebSocket(tmpUserInfo);
                                    }

                                    giftInfo.StreamInfoId = commentInfo.StreamInfoId;
                                    giftInfo.TimeStampUSec = commentInfo.TimeStampUSec;
                                    giftInfo.CommentId = commentInfo.CommentId;
                                    giftInfo.StreamSiteId = commentInfo.StreamSiteId;
                                    giftInfo.StreamManagementId = commentInfo.StreamManagementId;
                                    giftInfo.UserId = nicoadUserId;
                                    giftInfo.UserName = nicoadUserName;
                                    giftInfo.GiftType = "ニコニ広告";
                                    giftInfo.GiftValue = nicoadPoint;
                                    giftInfo.CommentText = nicoadMessage;

                                    // await GiftApi.PostNewCommentToDb(this.StreamManagementInfo.StreamNo, giftInfo);
                                    await this._commentApi.PostNewGiftToDbByWebSocket(this.StreamManagementInfo.StreamNo, giftInfo);
                                }
                            }

                            // ギフト
                            if (commentInfo.CommentText.IndexOf("/gift") > -1)
                            {
                                GiftInfo giftInfo = new GiftInfo();
                                UserInfo tmpUserInfo = new UserInfo();

                                int giftValue = -1, giftPrice = -1, giftCount = -1;
                                string tmpString, giftUserId = "", giftUserName = "", giftMessage = "";
                                string[] gitStrings = commentInfo.CommentText.Split(" ");

                                giftUserId = gitStrings[2];
                                giftUserName = gitStrings[3].Substring(1, gitStrings[3].Length - 2);

                                // ギフト価格
                                tmpString = gitStrings[4];
                                if (!int.TryParse(tmpString, out giftPrice)) { giftPrice = -1; }

                                // ギフト個数
                                if (gitStrings.Length > 7)
                                {
                                    tmpString = gitStrings[8];
                                    if (!int.TryParse(tmpString, out giftCount)) { giftCount = 1; }
                                }
                                else { giftCount = 1; }

                                giftValue = giftPrice * giftCount;

                                // ギフトメッセージ
                                tmpString = gitStrings[6].Substring(1, gitStrings[6].Length - 2);
                                giftMessage = tmpString + (giftCount > 0 ? "×" + giftCount.ToString() : "");

                                giftInfo.StreamInfoId = commentInfo.StreamInfoId;
                                giftInfo.TimeStampUSec = commentInfo.TimeStampUSec;
                                giftInfo.CommentId = commentInfo.CommentId;
                                giftInfo.StreamSiteId = commentInfo.StreamSiteId;
                                giftInfo.StreamManagementId = commentInfo.StreamManagementId;
                                giftInfo.UserId = giftUserId;
                                giftInfo.UserName = giftUserName;
                                giftInfo.GiftType = "ニコニコギフト";
                                giftInfo.GiftValue = giftValue;
                                giftInfo.CommentText = giftMessage;

                                tmpUserInfo.StreamSiteId = commentInfo.StreamSiteId;
                                tmpUserInfo.UserId = giftUserId;
                                tmpUserInfo.UserName = giftUserName;
                                tmpUserInfo.IconPath = await GetUserIcon(commentInfo.UserId, hiddenname);
                                tmpUserInfo.Note = gitStrings[2] == "NULL" ? "ニコニコギフト" : tmpUserInfo.Note;

                                // await UserApi.CreateOrUpdateUserInfo(tmpUserInfo);
                                await this._commentApi.PostNewUserToDbByWebSocket(tmpUserInfo);

                                // await GiftApi.PostNewCommentToDb(this.StreamManagementInfo.StreamNo, giftInfo);
                                await this._commentApi.PostNewGiftToDbByWebSocket(this.StreamManagementInfo.StreamNo, giftInfo);
                            }

                            // ユーザー情報取得
                            userInfo.StreamSiteId = commentInfo.StreamSiteId;
                            userInfo.UserId = commentInfo.UserId;
                            userInfo.UserName = commentInfo.UserName;
                            userInfo.IconPath = await GetUserIcon(commentInfo.UserId, hiddenname);

                            // DBへデータ登録
                            // WEB APIをリクエスト(ユーザー情報)
                            // await UserApi.CreateOrUpdateUserInfo(userInfo);
                            await this._commentApi.PostNewUserToDbByWebSocket(userInfo);

                            // WEB APIをリクエスト(コメント)
                            // await CommentApi.PostNewCommentToDb(this.StreamManagementInfo.StreamNo, commentInfo);
                            await this._commentApi.PostNewCommentToDbByWebSocket(this.StreamManagementInfo.StreamNo, commentInfo);

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
                        else if (responseJson.ContainsKey("thread"))
                        {
                            logger.Debug("response> Thread info :" + responseJson["thread"].ToString());
                        }
                        else
                        {
                            logger.Debug("response> " + message);
                        }

                        if (cancellationToken.IsCancellationRequested)
                        {
                            await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "OK", CancellationToken.None);
                            logger.Debug("サーバーから切断しました。");
                            break;
                        }
                    }
                }
                returnVal = 0;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "チャット情報の取得でエラーが発生しました。", ex.Message);
                logger.Debug(ex.StackTrace);
                returnVal = -1;
            }

            logger.Debug("========== Func End!　 ==================================================");
            return returnVal;
        }

        /// <summary>
        /// ユーザーアイコンの取得
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="hiddenName"></param>
        /// <returns></returns>
        private async Task<string> GetUserIcon(string userId, bool hiddenName)
        {
            string returnVal = "https://dcdn.cdn.nimg.jp/nicoaccount/usericon/{0}/{1}.jpg";
            logger.Trace("========== Func Start! ==================================================");

            try
            {
                if (hiddenName)
                {
                    returnVal = "https://secure-dcdn.cdn.nimg.jp/nicoaccount/usericon/defaults/blank_s.jpg";
                }
                else
                {
                    int fastDirNo = 0, tmpInt = 0;

                    if (int.TryParse(userId, out tmpInt))
                    {
                        returnVal = string.Format(returnVal, fastDirNo.ToString(), userId);

                        try
                        {
                        }
                        catch (Exception ex)
                        {
                            logger.Warn(ex, "ユーザーアイコン取得時にエラーが発生しました。エラーメッセージ:{0}", ex.Message);
                            returnVal = "https://secure-dcdn.cdn.nimg.jp/nicoaccount/usericon/defaults/blank_s.jpg";
                        }
                    }
                    else
                    {
                        returnVal = "https://secure-dcdn.cdn.nimg.jp/nicoaccount/usericon/defaults/blank_s.jpg";
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