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
    /// 配信情報処理APIラッパークラス
    /// </summary>
    public class StreamInfoApi
    {
        /// <summary>
        /// NLogロガー
        /// </summary>
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        /// <summary>
        /// 設定情報オブジェクト
        /// </summary>
        private static Setting setting = new Setting();

        /// <summary>
        /// 配信情報更新処理
        /// </summary>
        /// <param name="streamInfo"></param>
        /// <returns></returns>
        public static async Task<bool> UpdateStreamInfo(Models.StreamInfo streamInfo)
        {
            bool returnVal = false;
            string requestApiUrl = string.Format("{0}/{1}", setting.WebApiServerUrl.Replace("0.0.0.0", "localhost"), "streamInfo/Update");
            var json = JsonConvert.SerializeObject(streamInfo); ;

            logger.Debug("========== Func Start! ==================================================");
            logger.Debug("API要求先基本URL:{0}", requestApiUrl);
            logger.Trace("-------------------------------------------------------------------------");
            logger.Trace("・処理データ情報");
            logger.Trace("-------------------------------------------------------------------------");
            logger.Trace("配信ID                    :{0}", streamInfo.StreamId);
            logger.Trace("配信ID(配信サイト内ID)    :{0}", streamInfo.StreamPageId);
            logger.Trace("配信名                    :{0}", streamInfo.StreamName);
            logger.Trace("配信URL                   :{0}", streamInfo.StreamUrl);
            logger.Trace("コメント(チャット)ルームID:{0}", streamInfo.CommentRoomId);
            logger.Trace("配信サイトID              :{0}", streamInfo.StreamSiteId);
            logger.Trace("配信開始日時              :{0}", streamInfo.StartDateTime);
            logger.Trace("配信終了日時              :{0}", streamInfo.EndDateTime);
            logger.Trace("視聴者数                  :{0}", streamInfo.ViewerCount);
            logger.Trace("コメント数                :{0}", streamInfo.CommentCount);
            logger.Trace("配信ステータス            :{0}", streamInfo.LiveStatus);
            logger.Trace("備考                      :{0}", streamInfo.Note);
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

    }
}
