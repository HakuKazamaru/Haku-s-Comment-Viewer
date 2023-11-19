using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace HakuCommentViewer.Common.Models
{
    /// <summary>
    /// ユーザー情報管理データモデル
    /// </summary>
    [JsonObject]
    public class UserInfo
    {
        /// <summary>
        /// 配信サイトID
        /// </summary>
        [JsonProperty("StreamSiteId")]
        public string StreamSiteId { get; set; }

        /// <summary>
        /// ユーザーID
        /// </summary>
        [JsonProperty("UserId")]
        public string UserId { get; set; }

        /// <summary>
        /// ユーザー名
        /// </summary>
        [JsonProperty("UserName")]
        public string UserName { get; set; }

        /// <summary>
        /// ハンドルネーム
        /// </summary>
        [JsonProperty("HandleName")]
        public string? HandleName { get; set; }

        /// <summary>
        /// アイコン配置先
        /// </summary>
        [JsonProperty("IconPath")]
        public string? IconPath { get; set; }

        /// <summary>
        /// 累計コメント数
        /// </summary>
        [JsonProperty("CommentCount")]
        public int? CommentCount { get; set; }

        /// <summary>
        /// 累計スパ茶/ギフト数
        /// </summary>
        [JsonProperty("GiftCount")]
        public int? GiftCount { get; set; }

        /// <summary>
        /// 初回コメント日時
        /// </summary>
        [JsonProperty("FastCommentDateTime")]
        public DateTime? FastCommentDateTime { get; set; }

        /// <summary>
        /// 最終コメント日時
        /// </summary>
        [JsonProperty("LastCommentDateTime")]
        public DateTime? LastCommentDateTime { get; set; }

        /// <summary>
        /// 備考
        /// </summary>
        [JsonProperty("Note")]
        public string? Note { get; set; }
    }
}
