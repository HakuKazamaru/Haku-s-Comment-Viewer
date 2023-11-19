using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HakuCommentViewer.Plugin.Enums;
using HakuCommentViewer.Plugin.Interface;

namespace HakuCommentViewer.Plugin.Models
{
    /// <summary>
    /// プラグインインスタンス管理データモデル
    /// </summary>
    public class PluginInstance
    {
        /// <summary>
        /// コメント取得/送信プラグイン
        /// </summary>
        public IComment[] CommentPlugin { get; set; }

        /// <summary>
        /// 読上げプラグイン(ToDo)
        /// </summary>
        // public INarration[] CommentPlugin { get; set; }

        /// <summary>
        /// コメントお遊びプラグイン(ToDo)
        /// </summary>
        // public IHangOut[] HangOutPlugin { get; set; }

    }
}
