using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace HakuCommentViewer.Common.Models
{
    /// <summary>
    /// 配信サイト情報データモデル
    /// </summary>
    [JsonObject]
    public class StreamSiteInfo
    {
        /// <summary>
        /// 配信サイトID(GUID)
        /// </summary>
        [JsonProperty("StreamSiteId")]
        public string StreamSiteId { get; set; }

        /// <summary>
        /// 配信サイト名
        /// </summary>
        [JsonProperty("StreamSiteName")]
        public string StreamSiteName { get; set;}

        /// <summary>
        /// 配信サイトベースURL
        /// </summary>
        [JsonProperty("StreamSiteBaseUrl")]
        public string StreamSiteBaseUrl { get; set; }

        /// <summary>
        /// 配信URL判断パターン
        /// </summary>
        [JsonProperty("StreamUrlMuchPattern")]
        public string StreamUrlMuchPattern { get; set;}
    }
}
