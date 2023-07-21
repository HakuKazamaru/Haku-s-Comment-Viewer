using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Jint.Parser.Ast;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using NLog.LayoutRenderers;
using NLog.Web;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;

namespace YT_Test
{
    public static class Program
    {
        /// <summary>
        /// ロガー
        /// </summary>
        private static NLog.Logger logger;

        /// <summary>
        /// メインメソッド
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {
            // ロガーの設定読み込み
            try
            {
                string chatId = "";

                logger = NLogBuilder.ConfigureNLog("nlog.config").GetCurrentClassLogger();
                // logger = NLog.LogManager.Setup().LoadConfigurationFromAppSettings();
                logger.Info("========== App Start! ==================================================");

                // スクレイピング先URL
                string url = "https://youtube.com/live/3c8Rlf9NJOk";

                using (var getIdTask = Task.Run(() => GetChatID(url)))
                {
                    while (!getIdTask.IsCompleted)
                    {
                        Thread.Sleep(1000);
                    }
                    chatId = getIdTask.Result;

                    if (!string.IsNullOrEmpty(chatId))
                    {
                        using (var getChatTask = Task.Run(() => GetChatData(chatId, "3c8Rlf9NJOk")))
                        {
                            while (!getChatTask.IsCompleted)
                            {
                                Thread.Sleep(1000);
                            }

                            if (getChatTask.Result > -1)
                            {
                                logger.Info("チャット情報が取得できました。");
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

        /// <summary>
        /// 指定された配信URLのチャットIDを取得する
        /// </summary>
        /// <param name="url"></param>
        private static async Task<string> GetChatID(string url)
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

                var scriptDocs = responseData.GetElementsByTagName("script");
                foreach (var scriptDoc in scriptDocs)
                {
                    // logger.Trace("html：" + scriptDoc.InnerHtml);
                    if (scriptDoc.InnerHtml.IndexOf("var ytInitialData") != -1)
                    {
                        logger.Info("検索対象発見");
                        // logger.Trace("html:\r\n" + scriptDoc.InnerHtml);

                        string jsonString = scriptDoc.InnerHtml.Replace("var ytInitialData = ", "").Replace("};", "}");
                        // logger.Trace("json:\n" + jsonString);
                        JObject jsonObject = JObject.Parse(jsonString);

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
                            logger.Info("チャットID" + idString);

                            returnVal = idString;
                        }
                    }
                }

                if (returnVal == "")
                {
                    logger.Error("チャットIDが取得できませんでした。");
                }
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

        private static async Task<int> GetChatData(string chatId, string liveId)
        {
            int returnVal = -1;
            string apiUrl = "https://www.youtube.com/youtubei/v1/live_chat/get_live_chat";
            logger.Info("========== Func Start! ==================================================");

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

                logger.Debug("post data:\r\n" + postJsonString);

                using (var client = new HttpClient())
                {
                    var content = new StringContent(postJsonString, Encoding.UTF8, "application/json");
                    var response = await client.PostAsync(apiUrl, content);

                    logger.Debug("status Code:" + response.StatusCode);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        JObject jsonObject = JObject.Parse(responseContent);

                        returnVal = 0;
                    }
                    else
                    {
                        logger.Error("チャットデータの取得に失敗しました。");
                        returnVal = -1;
                    }
                }

            }
            catch (Exception ex)
            {
                logger.Error("チャットデータの取得でエラーが発生しました。");
                logger.Error(ex.Message);
                logger.Debug(ex.StackTrace);
                returnVal = -1;
            }

            logger.Info("========== Func End!　 ==================================================");
            return returnVal;
        }

    }
}

