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
    /// コメント処理APIラッパークラス
    /// </summary>
    public class CommentApi : IDisposable
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
        /// WebSocketコネクション管理オブジェクト
        /// </summary>
        private HubConnection _HubConnection { get; set; }

        /// <summary>
        /// WebSocket切断時の再接続試行回数
        /// </summary>
        public int RetryCount = 5;

        /// <summary>
        /// コンストラクター
        /// </summary>
        public CommentApi()
        {
            string requestWebSocketUrl = string.Format("{0}/{1}", setting.WebApiServerUrl.Replace("0.0.0.0", "localhost"), "commenthub");
            logger.Debug("========== Func Start! ==================================================");
            logger.Debug("WebSocket要求先URL:{0}", requestWebSocketUrl);
            this._HubConnection = new HubConnectionBuilder().WithUrl(requestWebSocketUrl).Build();
            this._HubConnection.Closed += async (exception) => { await ReConnectWebSocket(exception); };
            logger.Debug("========== Func End!   ==================================================");
        }

        /// <summary>
        /// 新規コメントDB登録処理
        /// </summary>
        /// <param name="streamNo"></param>
        /// <param name="commentInfo"></param>
        /// <returns></returns>
        public static async Task<bool> PostNewCommentToDb(int streamNo, Models.CommentInfo commentInfo)
        {
            bool returnVal = false;
            string requestApiUrl = string.Format("{0}/{1}", setting.WebApiServerUrl.Replace("0.0.0.0", "localhost"), "CommentInfo");
            var json = JsonConvert.SerializeObject(commentInfo); ;

            logger.Trace("========== Func Start! ==================================================");
            logger.Trace("API要求先基本URL:{0}", requestApiUrl);
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

            logger.Trace("========== Func End!   ==================================================");
            return returnVal;
        }

        /// <summary>
        /// コメント検索処理
        /// </summary>
        /// <param name="commentInfo"></param>
        /// <returns></returns>
        private static async Task<bool> CheckExitsCommentFromDb(Models.CommentInfo commentInfo)
        {
            bool returnVal = false;
            string requestApiUrl = string.Format("{0}/{1}", setting.WebApiServerUrl.Replace("0.0.0.0", "localhost"), "CommentInfo/Get");
            var json = JsonConvert.SerializeObject(commentInfo);

            logger.Trace("========== Func Start! ==================================================");
            logger.Trace("API要求先基本URL:{0}", requestApiUrl);
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

            logger.Trace("========== Func End!   ==================================================");
            return returnVal;
        }

        /// <summary>
        /// WebSocketコール処理
        /// </summary>
        /// <param name="streamNo"></param>
        /// <param name="commentInfo"></param>
        /// <returns></returns>
        private static async Task PostNewCommentToWebSocket(int streamNo, Models.CommentInfo commentInfo)
        {
            string requestWebSocketUrl = string.Format("{0}/{1}", setting.WebApiServerUrl.Replace("0.0.0.0", "localhost"), "commenthub");
            logger.Trace("========== Func Start! ==================================================");
            logger.Trace("WebSocket要求先URL:{0}", requestWebSocketUrl);

            var hubConnection = new HubConnectionBuilder().WithUrl(requestWebSocketUrl).Build();

            await hubConnection.StartAsync();
            await hubConnection.InvokeAsync(
                "ReceiveMessage",
                commentInfo.TimeStampUSec, commentInfo.CommentId, streamNo, commentInfo.UserId, commentInfo.UserName, commentInfo.CommentText);
            await hubConnection.StopAsync();
            await hubConnection.DisposeAsync();

            hubConnection = null;

            logger.Trace("========== Func End!   ==================================================");
        }

        /// <summary>
        /// WebSocket再接続処理
        /// </summary>
        /// <param name="exception"></param>
        /// <returns></returns>
        private async Task ReConnectWebSocket(Exception exception)
        {
            logger.Debug("========== Func Start! ==================================================");

            if (exception is not null)
            {
                bool result = false;
                // 再接続時
                logger.Warn("WebSockeの切断を検知しました。再接続を実施します。");

                for (int i = 0; i < this.RetryCount; i++)
                {
                    logger.Info($"[{i}]再接続中...");
                    await Task.Delay(1000);
                    try
                    {
                        await this._HubConnection.StartAsync();
                        logger.Info($"[{i}]再接続しました。");
                        result = true;
                        break;
                    }
                    catch (Exception ex)
                    {
                        logger.Warn(ex, $"[{i}]再接続に失敗しました。エラー:{ex.Message}");
                    }
                }

                if (!result) { logger.Error("再接続がリトライ上限に達しました。"); }
            }
            else
            {
                // 通常切断時
                logger.Info("WebSockeを切断しました。");
            }

            logger.Debug("========== Func End!   ==================================================");
        }

        /// <summary>
        /// WebSocket接続処理
        /// </summary>
        /// <returns></returns>
        private async Task ConnectWebSocket()
        {
            logger.Debug("========== Func Start! ==================================================");
            if (this._HubConnection.State != HubConnectionState.Connected &&
                this._HubConnection.State != HubConnectionState.Connecting)
            {
                await this._HubConnection.StartAsync();
                logger.Info("WebSockeを接続しました。");
            }
            else if (this._HubConnection.State == HubConnectionState.Connecting)
            {
                while (this._HubConnection.State == HubConnectionState.Connecting)
                {
                    await Task.Delay(250);
                }

                if (this._HubConnection.State != HubConnectionState.Connected)
                {
                    await this._HubConnection.StartAsync();
                    logger.Info("WebSockeを接続しました。");
                }
            }
            logger.Debug("========== Func End!   ==================================================");
        }

        /// <summary>
        /// コメント登録処理
        /// </summary>
        /// <param name="commentInfo"></param>
        /// <returns></returns>
        public async Task<bool> PostNewCommentToDbByWebSocket(int streamNo, Models.CommentInfo commentInfo)
        {
            bool returnVal = false;

            logger.Trace("========== Func Start! ==================================================");
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
                await ConnectWebSocket();

                await this._HubConnection.InvokeAsync("PostCommentToDb", streamNo, commentInfo);
                logger.Trace("コメントをDBに登録しました。");

                returnVal = true;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "予期せぬエラーが発生しました。エラーメッセージ:{0}", ex.Message);
                returnVal = false;
            }

            logger.Trace("========== Func End!   ==================================================");
            return returnVal;
        }

        /// <summary>
        /// コメント一括登録
        /// </summary>
        /// <param name="commentInfoList"></param>
        /// <returns></returns>
        public async Task<bool> PostNewCommentToDbByWebSocket(int streamNo, List<Models.CommentInfo> commentInfoList)
        {
            bool returnVal = false;

            logger.Trace("========== Func Start! ==================================================");
            logger.Trace("処理データ件数:{0}", commentInfoList.Count);

            try
            {
                await ConnectWebSocket();

                await this._HubConnection.InvokeAsync("PostCommentListToDb", streamNo, commentInfoList);
                logger.Trace("コメントをDBに登録しました。");

                returnVal = true;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "予期せぬエラーが発生しました。エラーメッセージ:{0}", ex.Message);
                returnVal = false;
            }

            logger.Trace("========== Func End!   ==================================================");
            return returnVal;
        }

        /// <summary>
        /// ユーザー登録
        /// </summary>
        /// <param name="userInfo"></param>
        /// <returns></returns>
        public async Task<bool> PostNewUserToDbByWebSocket(Models.UserInfo userInfo)
        {
            bool returnVal = false;

            logger.Trace("========== Func Start! ==================================================");
            logger.Trace("・処理データ情報");
            logger.Trace("-------------------------------------------------------------------------");
            logger.Trace("配信サイトID:{0}", userInfo.StreamSiteId);
            logger.Trace("ユーザーID  :{0}", userInfo.UserId);
            logger.Trace("ユーザー名  :{0}", userInfo.UserName);
            logger.Trace("アイコンURL :{0}", userInfo.IconPath);
            logger.Trace("コメント    :{0}", userInfo.Note);
            logger.Trace("-------------------------------------------------------------------------");

            try
            {
                await ConnectWebSocket();

                await this._HubConnection.InvokeAsync("PostUserToDb", userInfo);
                logger.Trace("ユーザーをDBに登録しました。");

                returnVal = true;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "予期せぬエラーが発生しました。エラーメッセージ:{0}", ex.Message);
                returnVal = false;
            }

            logger.Trace("========== Func End!   ==================================================");
            return returnVal;
        }

        /// <summary>
        /// ユーザー一括登録
        /// </summary>
        /// <param name="userInfoList"></param>
        /// <returns></returns>
        public async Task<bool> PostNewUserToDbByWebSocket(List<Models.UserInfo> userInfoList)
        {
            bool returnVal = false;

            logger.Trace("========== Func Start! ==================================================");
            logger.Trace("処理データ件数:{0}", userInfoList.Count);

            try
            {
                await ConnectWebSocket();

                await this._HubConnection.InvokeAsync("PostUserListToDb", userInfoList);
                logger.Trace("ユーザーをDBに登録しました。");

                returnVal = true;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "予期せぬエラーが発生しました。エラーメッセージ:{0}", ex.Message);
                returnVal = false;
            }

            logger.Trace("========== Func End!   ==================================================");
            return returnVal;
        }

        /// <summary>
        /// スパ茶/ギフト登録処理
        /// </summary>
        /// <param name="commentInfo"></param>
        /// <returns></returns>
        public async Task<bool> PostNewGiftToDbByWebSocket(int streamNo, Models.GiftInfo giftInfo)
        {
            bool returnVal = false;

            logger.Trace("========== Func Start! ==================================================");
            logger.Trace("・処理データ情報");
            logger.Trace("-------------------------------------------------------------------------");
            logger.Trace("配信ID          :{0}", giftInfo.StreamInfoId);
            logger.Trace("タイムスタンプ  :{0}", giftInfo.TimeStampUSec);
            logger.Trace("コメントID      :{0}", giftInfo.CommentId);
            logger.Trace("コメント        :{0}", giftInfo.CommentText);
            logger.Trace("配信管理ID      :{0}", giftInfo.StreamManagementId);
            logger.Trace("配信サイトID    :{0}", giftInfo.StreamSiteId);
            logger.Trace("ユーザーID      :{0}", giftInfo.UserId);
            logger.Trace("ユーザー名      :{0}", giftInfo.UserName);
            logger.Trace("ギフト種別      :{0}", giftInfo.GiftType);
            logger.Trace("ギフト金額      :{0}", giftInfo.GiftValue);
            logger.Trace("-------------------------------------------------------------------------");

            try
            {
                await ConnectWebSocket();

                await this._HubConnection.InvokeAsync("PostGiftToDb", streamNo, giftInfo);
                logger.Trace("スパ茶/ギフトをDBに登録しました。");

                returnVal = true;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "予期せぬエラーが発生しました。エラーメッセージ:{0}", ex.Message);
                returnVal = false;
            }

            logger.Trace("========== Func End!   ==================================================");
            return returnVal;
        }

        /// <summary>
        /// スパ茶/ギフト一括登録
        /// </summary>
        /// <param name="commentInfoList"></param>
        /// <returns></returns>
        public async Task<bool> PostNewGiftToDbByWebSocket(int streamNo, List<Models.GiftInfo> giftInfoList)
        {
            bool returnVal = false;

            logger.Trace("========== Func Start! ==================================================");
            logger.Trace("処理データ件数:{0}", giftInfoList.Count);

            try
            {
                await ConnectWebSocket();

                await this._HubConnection.InvokeAsync("PostGiftListToDb", streamNo, giftInfoList);
                logger.Trace("スパ茶/ギフトをDBに登録しました。");

                returnVal = true;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "予期せぬエラーが発生しました。エラーメッセージ:{0}", ex.Message);
                returnVal = false;
            }

            logger.Trace("========== Func End!   ==================================================");
            return returnVal;
        }

        /// <summary>
        /// デスコンストラクター
        /// </summary>
        public async void Dispose()
        {
            if (this._HubConnection is not null)
            {
                await this._HubConnection.StopAsync();
                await this._HubConnection.DisposeAsync();
            }
        }
    }
}
