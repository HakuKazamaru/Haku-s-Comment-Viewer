using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace HakuCommentViewer.Common.Models
{
    /// <summary>
    /// 配信情報データモデル
    /// </summary>
    [JsonObject]
    public class StreamInfo
    {
        /// <summary>
        /// 配信ID
        /// </summary>
        [JsonProperty("StreamId")]
        public string StreamId { get; set; }

        /// <summary>
        /// 配信名
        /// </summary>
        [JsonProperty("StreamName")]
        public string? StreamName { get; set; }

        /// <summary>
        /// 配信ID(配信サイト内ID)
        /// </summary>
        [JsonProperty("StreamPageId")]
        public string? StreamPageId { get; set; }

        /// <summary>
        /// 配信URL
        /// </summary>
        [JsonProperty("StreamUrl")]
        public string? StreamUrl { get; set; }

        /// <summary>
        /// コメント(チャット)ルームID
        /// </summary>
        [JsonProperty("CommentRoomId")]
        public string? CommentRoomId { get; set; }

        /// <summary>
        /// 配信サイトID
        /// </summary>
        [JsonProperty("StreamSiteId")]
        public string? StreamSiteId { get; set; }

        /// <summary>
        /// 配信開始日時
        /// </summary>
        [JsonProperty("StartDateTime")]
        public DateTime? StartDateTime { get; set; }

        /// <summary>
        /// 配信終了日時
        /// </summary>
        [JsonProperty("EndDateTime")]
        public DateTime? EndDateTime { get; set; }

        /// <summary>
        /// 視聴者数
        /// </summary>
        [JsonProperty("ViewerCount")]
        public int? ViewerCount {  get; set; }

        /// <summary>
        /// コメント数
        /// </summary>
        [JsonProperty("CommentCount")]
        public int? CommentCount { get; set; }

        /// <summary>
        /// 配信ステータス
        /// </summary>
        [JsonProperty("LiveStatus")]
        public int? LiveStatus { get; set; }

        /// <summary>
        /// 備考
        /// </summary>
        [JsonProperty("Note")]
        public string? Note { get; set; }
        
    }
}
