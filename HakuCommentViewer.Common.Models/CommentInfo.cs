using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace HakuCommentViewer.Common.Models
{
    /// <summary>
    /// コメント情報管理データモデル
    /// </summary>
    [JsonObject]
    public class CommentInfo
    {
        /// <summary>
        /// 配信情報ID
        /// </summary>
        [JsonProperty("StreamInfoId")]
        public string StreamInfoId { get; set; }

        /// <summary>
        /// コメント識別用
        /// </summary>
        [JsonProperty("TimeStampUSec")]
        public string TimeStampUSec { get; set; }

        /// <summary>
        /// コメント識別用
        /// </summary>
        [JsonProperty("CommentId")]
        public string CommentId { get; set; }

        /// <summary>
        /// 配信サイトID
        /// </summary>
        [JsonProperty("StreamSiteId")]
        public string StreamSiteId { get; set; }

        /// <summary>
        /// 配信管理番号
        /// </summary>
        [JsonProperty("StreamManagementId")]
        public string StreamManagementId { get; set; }

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
        /// コメント
        /// </summary>
        [JsonProperty("CommentText")]
        public string CommentText { get; set; }
    }
}
