using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HakuCommentViewer.Common.Models
{
    /// <summary>
    /// 配信情報管理データモデル
    /// </summary>
    [JsonObject]
    public class StreamManagementView
    {
        /// <summary>
        /// 配信管理ID(プライマリーキー)
        /// </summary>
        [JsonProperty("StreamManagementId")]
        public string StreamManagementId { get; set; }

        /// <summary>
        /// 配信管理番号(画面用)
        /// </summary>
        [JsonProperty("StreamNo")]
        public int StreamNo { get; set; }

        /// <summary>
        /// 配信情報ID
        /// </summary>
        [JsonProperty("StreamId")]
        public string StreamId { get; set; }

        /// <summary>
        /// 配信名
        /// </summary>
        [JsonProperty("StreamName")]
        public string StreamName { get; set; }

        /// <summary>
        /// 配信URL
        /// </summary>
        [JsonProperty("StreamUrl")]
        public string StreamUrl { get; set; }

        /// <summary>
        /// コメント(チャット)ルームID
        /// </summary>
        [JsonProperty("CommentRoomId")]
        public string CommentRoomId { get; set; }

        /// <summary>
        /// 配信サイトID
        /// </summary>
        [JsonProperty("StreamSiteId")]
        public string StreamSiteId { get; set; }

        /// <summary>
        /// 接続済みフラグ
        /// </summary>
        [JsonProperty("IsConnected")]
        public bool IsConnected { get; set; }

        /// <summary>
        /// コメントジェネレーター機能利用フラグ
        /// </summary>
        [JsonProperty("UseCommentGenerator")]
        public bool UseCommentGenerator { get; set; }

        /// <summary>
        /// 読上げ機能利用フラグ
        /// </summary>
        [JsonProperty("UseNarrator")]
        public bool UseNarrator { get; set; }

        /// <summary>
        /// 配信開始経過時間
        /// </summary>
        [JsonProperty("ElapsedTime")]
        public DateTime ElapsedTime { get; set; }

        /// <summary>
        /// 視聴者数
        /// </summary>
        [JsonProperty("ViewerCount")]
        public int ViewerCount { get; set; }

        /// <summary>
        /// コメント数
        /// </summary>
        [JsonProperty("CommentCount")]
        public int CommentCount { get; set; }

        /// <summary>
        /// 備考
        /// </summary>
        [JsonProperty("Note")]
        public string Note { get; set; }
    }
}
