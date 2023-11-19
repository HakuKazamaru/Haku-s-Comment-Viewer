using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HakuCommentViewer.Plugin.Models
{
    /// <summary>
    /// プラグインエラー情報連携用
    /// </summary>
    public class OnExcrptionArgs
    {
        /// <summary>
        /// エラーメッセージ
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 例外オブジェクト
        /// </summary>
        public Exception Exception { get; set; }

    }
}
