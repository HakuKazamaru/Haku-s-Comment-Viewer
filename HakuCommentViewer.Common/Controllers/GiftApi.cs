using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

using Microsoft.AspNetCore.SignalR.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HakuCommentViewer.Common.Controllers
{
    /// <summary>
    /// ギフト関連処理APIラッパークラス
    /// </summary>
    public class GiftApi
    {
        /// <summary>
        /// NLogロガー
        /// </summary>
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        /// <summary>
        /// 設定管理オブジェクト
        /// </summary>
        private static Setting setting = new Setting();

        /// <summary>
        /// 新規コメントDB登録処理
        /// </summary>
        /// <param name="streamNo"></param>
        /// <param name="commentInfo"></param>
        /// <returns></returns>
        public static async Task<bool> PostNewCommentToDb(int streamNo, Models.GiftInfo commentInfo)
        {
            bool returnVal = false;
            string requestApiUrl = string.Format("{0}/{1}", setting.WebApiServerUrl.Replace("0.0.0.0", "localhost"), "GiftInfo");
            var json = JsonConvert.SerializeObject(commentInfo); ;

            logger.Debug("========== Func Start! ==================================================");
            logger.Debug("API要求先基本URL:{0}", requestApiUrl);
            logger.Trace("-------------------------------------------------------------------------");
            logger.Trace("・処理データ情報");
            logger.Trace("-------------------------------------------------------------------------");
            logger.Trace("配信ID          :{0}", commentInfo.StreamInfoId);
            logger.Trace("タイムスタンプ  :{0}", commentInfo.TimeStampUSec);
            logger.Trace("コメントID      :{0}", commentInfo.CommentId);
            logger.Trace("コメント        :{0}", commentInfo.CommentText);
            logger.Trace("配信管理ID      :{0}", commentInfo.StreamManagementId);
            logger.Trace("配信サイトID    :{0}", commentInfo.StreamSiteId);
            logger.Trace("ユーザーID      :{0}", commentInfo.UserId);
            logger.Trace("ユーザー名      :{0}", commentInfo.UserName);
            logger.Trace("-------------------------------------------------------------------------");

            try
            {
                bool result = await CheckExitsCommentFromDb(commentInfo);

                if (!result)
                {
                    HttpClientHandler handler = new HttpClientHandler()
                    {
                        AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli
                    };

                    using (var client = new HttpClient(handler))
                    using (var content = new StringContent(json, Encoding.UTF8, "application/json"))
                    {
                        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));

                        client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
                        client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));
                        client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("br"));

                        client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("ja"));
                        client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("en"));
                        client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("en-US"));

                        using (var response = await client.PostAsync(requestApiUrl, content))
                        {
                            string head = response.Headers.ToString();
                            string body = await response.Content.ReadAsStringAsync();
                            if (response.IsSuccessStatusCode)
                            {
                                logger.Debug("要求が成功しました。レスポンスコード:{0}", response.StatusCode);
                                returnVal = true;
                            }
                            else
                            {
                                logger.Warn("要求が失敗しました。レスポンスコード:{0}", response.StatusCode);
                            }
                            logger.Debug("レスポンス内容:\r\nhead:\r\n{0}\r\nbody:\r\n{1}", head, body);
                        }
                    }

                    await PostNewCommentToWebSocket(streamNo, commentInfo);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "予期せぬエラーが発生しました。エラーメッセージ:{0}", ex.Message);
                returnVal = false;
            }

            logger.Debug("========== Func End!   ==================================================");
            return returnVal;
        }

        /// <summary>
        /// コメント検索処理
        /// </summary>
        /// <param name="commentInfo"></param>
        /// <returns></returns>
        private static async Task<bool> CheckExitsCommentFromDb(Models.GiftInfo commentInfo)
        {
            bool returnVal = false;
            string requestApiUrl = string.Format("{0}/{1}", setting.WebApiServerUrl.Replace("0.0.0.0", "localhost"), "GiftInfo/Get");
            var json = JsonConvert.SerializeObject(commentInfo);

            logger.Debug("========== Func Start! ==================================================");
            logger.Debug("API要求先基本URL:{0}", requestApiUrl);
            logger.Trace("-------------------------------------------------------------------------");
            logger.Trace("・処理データ情報");
            logger.Trace("-------------------------------------------------------------------------");
            logger.Trace("配信ID          :{0}", commentInfo.StreamInfoId);
            logger.Trace("タイムスタンプ  :{0}", commentInfo.TimeStampUSec);
            logger.Trace("コメントID      :{0}", commentInfo.CommentId);
            logger.Trace("-------------------------------------------------------------------------");

            try
            {
                HttpClientHandler handler = new HttpClientHandler()
                {
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli
                };

                using (var client = new HttpClient(handler))
                using (var content = new StringContent(json, Encoding.UTF8, "application/json"))
                {
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));

                    client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
                    client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));
                    client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("br"));

                    client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("ja"));
                    client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("en"));
                    client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("en-US"));

                    // リクエストパラメータ生成
                    requestApiUrl = string.Format("{0}?streamInfoId={1}&timeStampUSec={2}&commentId={3}",
                        requestApiUrl, commentInfo.StreamInfoId, commentInfo.TimeStampUSec, commentInfo.CommentId);
                    logger.Debug("API要求先URL    :{0}", requestApiUrl);

                    using (var response = await client.GetAsync(requestApiUrl))
                    {
                        string head = response.Headers.ToString();
                        string body = "";

                        if (response.IsSuccessStatusCode)
                        {
                            logger.Debug("要求が成功しました。レスポンスコード:{0}", response.StatusCode);

                            using (var stream = await response.Content.ReadAsStreamAsync())
                            using (var reader = new StreamReader(stream, Encoding.GetEncoding("UTF-8"), true) as TextReader)
                            {
                                body = await reader.ReadToEndAsync();
                            }

                            if (!string.IsNullOrWhiteSpace(body))
                            {
                                JObject jsonObject = JObject.Parse(body);
                                logger.Debug("データーは登録済みです。");
                                returnVal = true;
                            }
                            else
                            {
                                logger.Debug("データーは未登録です。");
                            }
                        }
                        else
                        {
                            logger.Warn("要求が失敗しました。レスポンスコード:{0}", response.StatusCode);
                        }
                        logger.Debug("レスポンス内容:\r\nhead:\r\n{0}\r\nbody:\r\n{1}", head, body);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "予期せぬエラーが発生しました。エラーメッセージ:{0}", ex.Message);
                returnVal = false;
            }

            logger.Debug("========== Func End!   ==================================================");
            return returnVal;
        }

        /// <summary>
        /// WebSocketコール処理
        /// </summary>
        /// <param name="streamNo"></param>
        /// <param name="commentInfo"></param>
        /// <returns></returns>
        private static async Task PostNewCommentToWebSocket(int streamNo, Models.GiftInfo commentInfo)
        {
            string requestWebSocketUrl = string.Format("{0}/{1}", setting.WebApiServerUrl.Replace("0.0.0.0", "localhost"), "commenthub");
            logger.Debug("========== Func Start! ==================================================");
            logger.Debug("WebSocket要求先URL:{0}", requestWebSocketUrl);

            var hubConnection = new HubConnectionBuilder().WithUrl(requestWebSocketUrl).Build();

            await hubConnection.StartAsync();
            await hubConnection.InvokeAsync(
                "ReceiveGift",
                commentInfo.TimeStampUSec, commentInfo.CommentId, streamNo, commentInfo.UserId, commentInfo.UserName, commentInfo.GiftType, commentInfo.GiftValue, commentInfo.CommentText);
            await hubConnection.StopAsync();
            await hubConnection.DisposeAsync();

            hubConnection = null;

            logger.Debug("========== Func End!   ==================================================");
        }

    }
}
