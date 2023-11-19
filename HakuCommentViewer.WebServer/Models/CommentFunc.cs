using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HakuCommentViewer.Plugin;

namespace HakuCommentViewer.WebServer.Models
{
    public class CommentFunc
    {
        /// <summary>
        /// キャンセルトークンソース
        /// </summary>
        public CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        /// <summary>
        /// コメント取得オブジェクト
        /// </summary>
        public Plugin.Interface.IComment? Comment { get; set; }
    }
}
