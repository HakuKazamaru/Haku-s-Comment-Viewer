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
    /// 配信管理処理APIラッパークラス
    /// </summary>
    public class StreamManagementApi
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
        /// コネクションステータス取得処理
        /// </summary>
        /// <param name="streamManagementInfo"></param>
        /// <returns></returns>
        public static async Task<Models.StreamManagementInfo> GetData(string streamManagementId)
        {
            Models.StreamManagementInfo returnVal = new Models.StreamManagementInfo();
            string requestApiUrl = string.Format("{0}/{1}", setting.WebApiServerUrl.Replace("0.0.0.0", "localhost"), "StreamManagementInfo/Get");

            logger.Trace("========== Func Start! ==================================================");
            logger.Trace("API要求先基本URL:{0}", requestApiUrl);
            logger.Trace("-------------------------------------------------------------------------");
            logger.Trace("・処理データ情報");
            logger.Trace("-------------------------------------------------------------------------");
            logger.Trace("配信管理ID      :{0}", streamManagementId);
            logger.Trace("-------------------------------------------------------------------------");

            try
            {
                HttpClientHandler handler = new HttpClientHandler()
                {
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli
                };

                using (var client = new HttpClient(handler))
                {
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));

                    client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
                    client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));
                    client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("br"));

                    client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("ja"));
                    client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("en"));
                    client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("en-US"));

                    // リクエストパラメータ生成
                    requestApiUrl = string.Format("{0}?id={1}", requestApiUrl, streamManagementId);
                    logger.Trace("API要求先URL    :{0}", requestApiUrl);


                    using (var response = await client.GetAsync(requestApiUrl))
                    {
                        string head = response.Headers.ToString();
                        string body = "";

                        if (response.IsSuccessStatusCode)
                        {
                            logger.Trace("要求が成功しました。レスポンスコード:{0}", response.StatusCode);

                            using (var stream = await response.Content.ReadAsStreamAsync())
                            using (var reader = new StreamReader(stream, Encoding.GetEncoding("UTF-8"), true) as TextReader)
                            {
                                body = await reader.ReadToEndAsync();
                            }

                            if (!string.IsNullOrWhiteSpace(body))
                            {
                                JObject jsonObject = JObject.Parse(body);
                                logger.Trace("データーは登録済みです。");
                                returnVal.StreamManagementId = (string)jsonObject["streamManagementId"];
                                returnVal.StreamId = (string?)jsonObject["streamId"];
                                returnVal.StreamNo = (int)jsonObject["streamNo"];
                                returnVal.IsConnected = (bool)jsonObject["isConnected"];
                                returnVal.ControlRequest = (int?)jsonObject["controlRequest"];
                                returnVal.UseCommentGenerator = (bool)jsonObject["useCommentGenerator"];
                                returnVal.UseNarrator = (bool)jsonObject["useNarrator"];
                                returnVal.Note = (string?)jsonObject["note"];
                            }
                            else
                            {
                                logger.Trace("データーは未登録です。");
                                returnVal = null;
                            }
                        }
                        else
                        {
                            logger.Warn("要求が失敗しました。レスポンスコード:{0}", response.StatusCode);
                        }
                        logger.Trace("レスポンス内容:\r\nhead:\r\n{0}\r\nbody:\r\n{1}", head, body);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "予期せぬエラーが発生しました。エラーメッセージ:{0}", ex.Message);
                returnVal = null;
            }

            logger.Trace("========== Func End!   ==================================================");
            return returnVal;
        }


        /// <summary>
        /// コネクションステータス更新処理
        /// </summary>
        /// <param name="streamManagementInfo"></param>
        /// <returns></returns>
        public static async Task<bool> SetConnectionStatus(Models.StreamManagementInfo streamManagementInfo)
        {
            bool returnVal = false;
            string requestApiUrl = string.Format("{0}/{1}", setting.WebApiServerUrl.Replace("0.0.0.0", "localhost"), "StreamManagementInfo/Update");
            var json = JsonConvert.SerializeObject(streamManagementInfo); ;

            logger.Debug("========== Func Start! ==================================================");
            logger.Debug("API要求先基本URL:{0}", requestApiUrl);
            logger.Trace("-------------------------------------------------------------------------");
            logger.Trace("・処理データ情報");
            logger.Trace("-------------------------------------------------------------------------");
            logger.Trace("配信管理ID      :{0}", streamManagementInfo.StreamManagementId);
            logger.Trace("配信ID          :{0}", streamManagementInfo.StreamId);
            logger.Trace("配信管理NO      :{0}", streamManagementInfo.StreamNo);
            logger.Trace("コメント        :{0}", streamManagementInfo.IsConnected);
            logger.Trace("ｺﾒｼﾞｪﾈ使用フラグ:{0}", streamManagementInfo.UseCommentGenerator);
            logger.Trace("読上げ使用フラグ:{0}", streamManagementInfo.UseNarrator);
            logger.Trace("備考            :{0}", streamManagementInfo.Note);
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
        /// コネクションステータス取得処理
        /// </summary>
        /// <param name="streamManagementInfo"></param>
        /// <returns></returns>
        public static async Task<bool> GetConnectionStatus(Models.StreamManagementInfo streamManagementInfo)
        {
            bool returnVal = false;
            string requestApiUrl = string.Format("{0}/{1}", setting.WebApiServerUrl.Replace("0.0.0.0", "localhost"), "StreamManagementInfo/Get");
            var json = JsonConvert.SerializeObject(streamManagementInfo); ;

            logger.Debug("========== Func Start! ==================================================");
            logger.Debug("API要求先基本URL:{0}", requestApiUrl);
            logger.Trace("-------------------------------------------------------------------------");
            logger.Trace("・処理データ情報");
            logger.Trace("-------------------------------------------------------------------------");
            logger.Trace("配信管理ID      :{0}", streamManagementInfo.StreamManagementId);
            logger.Trace("配信ID          :{0}", streamManagementInfo.StreamId);
            logger.Trace("配信管理NO      :{0}", streamManagementInfo.StreamNo);
            logger.Trace("コメント        :{0}", streamManagementInfo.IsConnected);
            logger.Trace("ｺﾒｼﾞｪﾈ使用フラグ:{0}", streamManagementInfo.UseCommentGenerator);
            logger.Trace("読上げ使用フラグ:{0}", streamManagementInfo.UseNarrator);
            logger.Trace("備考            :{0}", streamManagementInfo.Note);
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
                    requestApiUrl = string.Format("{0}?id={1}", requestApiUrl, streamManagementInfo.StreamManagementId);
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
                                returnVal = (bool)jsonObject["isConnected"];
                            }
                            else
                            {
                                logger.Debug("データーは未登録です。");
                                returnVal = false;
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
        /// WebSocketコール処理(接続処理結果通知)
        /// </summary>
        /// <param name="streamManagementInfo"></param>
        /// <param name="result"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static async Task SendConnectionStartStatusToWebSocket(Models.StreamManagementInfo streamManagementInfo, bool result, string message = "")
        {
            string requestWebSocketUrl = string.Format("{0}/{1}", setting.WebApiServerUrl.Replace("0.0.0.0", "localhost"), "commenthub");
            logger.Debug("========== Func Start! ==================================================");
            logger.Debug("WebSocket要求先URL:{0}", requestWebSocketUrl);

            var hubConnection = new HubConnectionBuilder().WithUrl(requestWebSocketUrl).Build();

            await hubConnection.StartAsync();
            await hubConnection.InvokeAsync("StartResult", streamManagementInfo.StreamNo.ToString(), result, message);
            await hubConnection.StopAsync();
            await hubConnection.DisposeAsync();

            hubConnection = null;

            logger.Debug("========== Func End!   ==================================================");
        }

        /// <summary>
        /// WebSocketコール処理(切断処理結果通知)
        /// </summary>
        /// <param name="streamManagementInfo"></param>
        /// <returns></returns>
        public static async Task SendConnectionStopStatusToWebSocket(Models.StreamManagementInfo streamManagementInfo)
        {
            string requestWebSocketUrl = string.Format("{0}/{1}", setting.WebApiServerUrl.Replace("0.0.0.0", "localhost"), "commenthub");
            logger.Debug("========== Func Start! ==================================================");
            logger.Debug("WebSocket要求先URL:{0}", requestWebSocketUrl);

            var hubConnection = new HubConnectionBuilder().WithUrl(requestWebSocketUrl).Build();

            await hubConnection.StartAsync();
            await hubConnection.InvokeAsync("Stoped", streamManagementInfo.StreamNo.ToString());
            await hubConnection.StopAsync();
            await hubConnection.DisposeAsync();

            hubConnection = null;

            logger.Debug("========== Func End!   ==================================================");
        }

    }
}
