using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using HakuCommentViewer.Common.Models;

namespace HakuCommentViewer.Common.Controllers
{
    public class UserApi
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
        /// ユーザー情報登録
        /// </summary>
        /// <param name="userInfo"></param>
        /// <returns></returns>
        public static async Task<bool> CreateOrUpdateUserInfo(Models.UserInfo userInfo)
        {
            bool returnVal = false;
            string requestApiUrl = string.Format("{0}/{1}", setting.WebApiServerUrl.Replace("0.0.0.0", "localhost"), "UserInfo");
            var json = JsonConvert.SerializeObject(userInfo); ;

            logger.Debug("========== Func Start! ==================================================");
            logger.Debug("API要求基礎URL:{0}", requestApiUrl);
            logger.Trace("-------------------------------------------------------------------------");
            logger.Trace("・処理データ情報");
            logger.Trace("-------------------------------------------------------------------------");
            logger.Trace("配信サイトID :{0}", userInfo.StreamSiteId);
            logger.Trace("ユーザーID   :{0}", userInfo.UserId);
            logger.Trace("ユーザー名   :{0}", userInfo.UserName);
            logger.Trace("アイコンパス :{0}", userInfo.IconPath);
            logger.Trace("備考         :{0}", userInfo.Note);
            logger.Trace("-------------------------------------------------------------------------");

            try
            {
                bool result = await CheckExitsUserFromDb(userInfo);

                if (!result)
                {
                    // 新規登録
                    requestApiUrl = requestApiUrl + "/Create";
                }
                else
                {
                    // 更新
                    requestApiUrl = requestApiUrl + "/Update";
                }
                logger.Debug("API要求URL    :{0}", requestApiUrl);

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
        /// ユーザー情報存在確認
        /// </summary>
        /// <param name="userInfo"></param>
        /// <returns></returns>
        private static async Task<bool> CheckExitsUserFromDb(Models.UserInfo userInfo)
        {
            bool returnVal = false;
            string requestApiUrl = string.Format("{0}/{1}", setting.WebApiServerUrl.Replace("0.0.0.0", "localhost"), "UserInfo/Get");
            var json = JsonConvert.SerializeObject(userInfo);

            logger.Debug("========== Func Start! ==================================================");
            logger.Debug("API要求先基本URL:{0}", requestApiUrl);
            logger.Trace("-------------------------------------------------------------------------");
            logger.Trace("・処理データ情報");
            logger.Trace("-------------------------------------------------------------------------");
            logger.Trace("配信サイトID    :{0}", userInfo.StreamSiteId);
            logger.Trace("ユーザーID      :{0}", userInfo.UserId);
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
                    requestApiUrl = string.Format("{0}?streamSiteId={1}&userId={2}",
                        requestApiUrl, userInfo.StreamSiteId, userInfo.UserId);
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
        /// ユーザー情報存在確認
        /// </summary>
        /// <param name="userInfo"></param>
        /// <returns></returns>
        public static async Task<UserInfo> GetUserInfoFromDbByName(Models.UserInfo userInfo)
        {
            UserInfo returnVal = null;
            string requestApiUrl = string.Format("{0}/{1}", setting.WebApiServerUrl.Replace("0.0.0.0", "localhost"), "UserInfo/GetByName");
            var json = JsonConvert.SerializeObject(userInfo);

            logger.Debug("========== Func Start! ==================================================");
            logger.Debug("API要求先基本URL:{0}", requestApiUrl);
            logger.Trace("-------------------------------------------------------------------------");
            logger.Trace("・処理データ情報");
            logger.Trace("-------------------------------------------------------------------------");
            logger.Trace("配信サイトID    :{0}", userInfo.StreamSiteId);
            logger.Trace("ユーザー名      :{0}", userInfo.UserName);
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
                    requestApiUrl = string.Format("{0}?streamSiteId={1}&userName={2}",
                        requestApiUrl, userInfo.StreamSiteId, userInfo.UserId);
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
                                returnVal.StreamSiteId = jsonObject["StreamSiteId"].ToString();
                                returnVal.UserId = jsonObject["UserId"].ToString();
                                returnVal.UserName = jsonObject["UserName"].ToString();
                                returnVal.IconPath = jsonObject["IconPath"].ToString();
                                returnVal.Note = jsonObject["Note"].ToString();
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
                returnVal = null;
            }

            logger.Debug("========== Func End!   ==================================================");
            return returnVal;
        }
    }
}
