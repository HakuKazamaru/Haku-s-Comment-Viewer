using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HakuCommentViewer.Plugin.Enums;

namespace HakuCommentViewer.Plugin.Models
{
    /// <summary>
    /// プラグイン情報データモデル
    /// </summary>
    public class PluginInfo
    {
        /// <summary>
        /// プラグイン管理ID(GUID)
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// プラグイン名
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// プラグイン種別
        /// </summary>
        public PluginType Type { get; set; }

        /// <summary>
        /// プラグイン動作状況
        /// </summary>
        public PluginStatus Status { get; set; }

        /// <summary>
        /// プラグイン作者名
        /// </summary>
        public string Author { get; set; }

        /// <summary>
        /// プラグイン説明
        /// </summary>
        public string Description { get; set; }
    }
}
