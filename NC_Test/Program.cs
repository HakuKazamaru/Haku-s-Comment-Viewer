using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Newtonsoft.Json.Linq;
using NLog.Web;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json.Nodes;

namespace NC_Test
{
    /// <summary>
    /// メインクラス
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// ロガー
        /// </summary>
        private static NLog.Logger logger;

        /// <summary>
        /// 並列処理停止フラグ
        /// </summary>
        private static bool cancelFlag = false;

        /// <summary>
        /// コメントチャンネルID
        /// </summary>
        private static string threadId = "";

        /// <summary>
        /// comment投稿Key
        /// </summary>
        private static string threadKey = "";

        /// <summary>
        /// メインメソッド
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {
            try
            {
                // ロガーの設定読み込み
                logger = NLogBuilder.ConfigureNLog("nlog.config").GetCurrentClassLogger();
                // logger = NLog.LogManager.Setup().LoadConfigurationFromAppSettings();
                logger.Info("========== App Start! ==================================================");

                // スクレイピング先URL
                string url = "https://live.nicovideo.jp/watch/lv342002655";

                using (var getIdTask = Task.Run(() => GetWebsocketUrl(url)))
                {
                    string websocketUrl = "";
                    while (!getIdTask.IsCompleted)
                    {
                        Thread.Sleep(1000);
                    }
                    websocketUrl = getIdTask.Result;

                    if (!string.IsNullOrEmpty(websocketUrl))
                    {
                        using (var checkKeyStatu = Task.Run(() => CheckEscapeKey()))
                        using (var getChatInfoTask = Task.Run(() => GetChatInfo(websocketUrl)))
                        {
                            logger.Info("終了するにはエスケープキーを押してください。");

                            while (true)
                            {

                                if (!string.IsNullOrEmpty(threadId))
                                {
                                    logger.Info("チャットチャンネルIDを取得しました。ID：" + threadId);
                                    using (var getChatDataTask = Task.Run(() => GetChatData(threadId, "")))
                                    {
                                        while (true)
                                        {
                                            if (cancelFlag)
                                            {
                                                logger.Info("チャット取得処理の中断を開始します。");
                                                getChatDataTask.Wait();
                                                logger.Info("チャット取得処理を中断しました。");
                                                break;
                                            }

                                            Thread.Sleep(250);
                                        }
                                    }
                                }

                                if (cancelFlag)
                                {
                                    logger.Info("処理の中断を開始します。");
                                    getChatInfoTask.Wait();
                                    logger.Info("処理を中断しました。");

                                    if (getChatInfoTask.Result > -1)
                                    {
                                        logger.Info("チャット情報が取得できました。");
                                    }
                                    else
                                    {
                                        logger.Info("チャット情報が取得できませんでした。");
                                    }

                                    cancelFlag = false;

                                    logger.Info("処理を終了します。");
                                    break;
                                }

                                Thread.Sleep(250);
                            }

                        }
                    }
                }

                logger.Info("========== App End!　 ==================================================");

            }
            catch (Exception ex)
            {
                Console.WriteLine("ロガーの設定の読み込みに失敗しました。");
                Console.WriteLine("エラーメッセージ：" + ex.Message);
            }
        }

        private static async Task<bool> CheckEscapeKey()
        {
            bool returnVal = false;
            logger.Info("========== Func Start! ==================================================");
            while (true)
            {
                ConsoleKeyInfo consoleKeyInfo = Console.ReadKey();

                if (consoleKeyInfo.Key == ConsoleKey.Escape)
                {
                    logger.Info("エスケープキーが押下されました。");
                    cancelFlag = true;
                    break;
                }

                Thread.Sleep(250);
            }
            logger.Info("========== Func End!　 ==================================================");
            return returnVal;
        }


        /// <summary>
        /// ChatAPI URL取得メソッド
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private static async Task<string> GetWebsocketUrl(string url)
        {
            string returnVal = "";
            logger.Info("========== Func Start! ==================================================");

            try
            {
                var responseData = default(IHtmlDocument);

                logger.Info("取得先URL：" + url);

                // スクレイピング
                using (var client = new HttpClient())
                using (var stream = await client.GetStreamAsync(new Uri(url)))
                {
                    var parser = new HtmlParser();
                    responseData = await parser.ParseDocumentAsync(stream);
                }

                logger.Info("ページタイトル：" + responseData.Title);

                string scriptData = responseData.GetElementById("embedded-data").Attributes["data-props"].Value;
                // logger.Trace("json:\n" + jsonString);
                JObject jsonObject = JObject.Parse(scriptData);

                string wesocketUrl = jsonObject["site"]["relive"]["webSocketUrl"].ToString();
                returnVal = wesocketUrl;

            }
            catch (Exception ex)
            {
                logger.Error("チャットIDの取得でエラーが発生しました。");
                logger.Error(ex.Message);
                logger.Debug(ex.StackTrace);
                returnVal = "";
            }

            logger.Info("========== Func End!　 ==================================================");
            return returnVal;
        }

        /// <summary>
        /// チャットデータの取得を開始
        /// </summary>
        /// <param name="websocketUrl"></param>
        /// <returns></returns>
        private static async Task<int> GetChatInfo(string websocketUrl)
        {
            int returnVal = -1;
            logger.Info("========== Func Start! ==================================================");

            try
            {
                logger.Info("取得先URL：" + websocketUrl);

                using (var client = new ClientWebSocket())
                {
                    int opened = 0;

                    // client.Options.SetRequestHeader("Sec-WebSocket-Protocol", "msg.nicovideo.jp#json");
                    client.Options.SetRequestHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/114.0.0.0 Safari/537.36 Edg/114.0.1823.67");
                    await client.ConnectAsync(new Uri(websocketUrl), CancellationToken.None);

                    using (var chatTask = new Task(() => GetChatData("", "")))
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
                                        logger.Info("post>" + sendString);

                                        opened = 1;
                                        break;
                                    }
                                case 1:
                                    {
                                        string sendString = @"{""type"":""getAkashic"",""data"":{""chasePlay"":false}}";
                                        var tmpBuffer = Encoding.UTF8.GetBytes(sendString);
                                        var tmpSegment = new ArraySegment<byte>(tmpBuffer);

                                        await client.SendAsync(tmpSegment, WebSocketMessageType.Text, true, CancellationToken.None);
                                        logger.Info("post>" + sendString);

                                        opened = 2;
                                        break;
                                    }
                                default:
                                    {
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
                                    {
                                        string sendString = @"{""type"":""pong""}";
                                        logger.Info("response> Ping!");
                                        var tmpBuffer = Encoding.UTF8.GetBytes(sendString);
                                        var tmpSegment = new ArraySegment<byte>(tmpBuffer);

                                        await client.SendAsync(tmpSegment, WebSocketMessageType.Text, true, CancellationToken.None);
                                        logger.Info("post>" + sendString);

                                        break;
                                    }
                                case "statistics":
                                    {
                                        logger.Info("response> Statistics info :" + responseJson["data"].ToString());
                                        break;
                                    }
                                case "akashic":
                                    {
                                        logger.Info("response> Akashic info :" + responseJson["data"].ToString());
                                        break;
                                    }
                                case "room":
                                    {
                                        logger.Info("response> Room info :" + responseJson["data"].ToString());
                                        threadId = responseJson["data"]["threadId"].ToString();
                                        break;
                                    }
                                case "schedule":
                                    {
                                        logger.Info("response> Schedule info :" + responseJson["data"].ToString());
                                        break;
                                    }
                                case "stream":
                                    {
                                        logger.Info("response> Stream info :" + responseJson["data"].ToString());
                                        break;
                                    }
                                case "seat":
                                    {
                                        logger.Info("response> Seat keep interval sec :" + responseJson["data"]["keepIntervalSec"].ToString());
                                        break;
                                    }
                                case "serverTime":
                                    {
                                        logger.Info("response> Sever Time :" + responseJson["data"]["currentMs"].ToString());
                                        break;
                                    }
                                default:
                                    {
                                        logger.Info("response> " + message);
                                        break;
                                    }
                            }

                            if (cancelFlag)
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
                logger.Error("チャット情報の取得でエラーが発生しました。");
                logger.Error(ex.Message);
                logger.Debug(ex.StackTrace);
                returnVal = -1;
            }

            logger.Info("========== Func End!　 ==================================================");
            return returnVal;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="threadId"></param>
        /// <param name="threadKey"></param>
        /// <returns></returns>
        private static async Task<int> GetChatData(string threadId, string threadKey)
        {
            int returnVal = -1;
            const string websocketUrl = "wss://msgd.live2.nicovideo.jp/websocket";
            logger.Info("========== Func Start! ==================================================");

            try
            {
                logger.Info("取得先URL：" + websocketUrl);

                using (var client = new ClientWebSocket())
                {
                    int opened = 0;

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
                                    /*
                                    string sendString = @"
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
                                    */
                                    string sendString = @"
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

                                    sendString = sendString.Replace("@threadId", threadId).Replace("@threadId", threadKey);

                                    var tmpBuffer = Encoding.UTF8.GetBytes(sendString);
                                    var tmpSegment = new ArraySegment<byte>(tmpBuffer);

                                    await client.SendAsync(tmpSegment, WebSocketMessageType.Text, true, CancellationToken.None);
                                    logger.Info("post>" + sendString);

                                    opened = 1;
                                    break;
                                }
                            default:
                                {
                                    break;
                                }
                        }

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
                            string sendString = @"{""type"":""pong""}";
                            logger.Info("response> Ping!");
                            var tmpBuffer = Encoding.UTF8.GetBytes(sendString);
                            var tmpSegment = new ArraySegment<byte>(tmpBuffer);

                            await client.SendAsync(tmpSegment, WebSocketMessageType.Text, true, CancellationToken.None);
                            logger.Info("post>" + sendString);

                            sendString = @"{""type"":""keepSeat""}";
                            logger.Info("response> Ping!");
                            tmpBuffer = Encoding.UTF8.GetBytes(sendString);
                            tmpSegment = new ArraySegment<byte>(tmpBuffer);

                            await client.SendAsync(tmpSegment, WebSocketMessageType.Text, true, CancellationToken.None);
                            logger.Info("post>" + sendString);
                        }
                        else if (responseJson.ContainsKey("chat"))
                        {
                            logger.Info("response> Chat info :" + responseJson["chat"].ToString());
                        }
                        else if (responseJson.ContainsKey("thread"))
                        {
                            logger.Info("response> Thread info :" + responseJson["thread"].ToString());
                        }
                        else
                        {
                            logger.Info("response> " + message);
                        }

                        if (cancelFlag)
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
                logger.Error("チャット情報の取得でエラーが発生しました。");
                logger.Error(ex.Message);
                logger.Debug(ex.StackTrace);
                returnVal = -1;
            }


            logger.Info("========== Func End!　 ==================================================");
            return returnVal;
        }

    }
}


