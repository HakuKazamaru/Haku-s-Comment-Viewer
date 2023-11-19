using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HakuCommentViewer.Plugin.Models
{
    public class GetNewGiftArgs
    {
        /// <summary>
        /// コメント一意化用
        /// </summary>
        public string TimeStampUSec { get; set; }

        /// <summary>
        /// コメント一意化用
        /// </summary>
        public string CommentId { get; set; }

        /// <summary>
        /// 配信管理No
        /// </summary>
        public int StreamNo { get; set; }

        /// <summary>
        /// ユーザーID
        /// </summary>
        public string UserId { get; set; }
        
        /// <summary>
        /// ユーザー名
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// スパ茶・ギフト種別
        /// </summary>
        public string GiftType { get; set; }

        /// <summary>
        /// スパ茶・ギフト金額
        /// </summary>
        public int GiftValue { get; set; }

        /// <summary>
        /// コメント
        /// </summary>
        public string CommentText { get; set; }
    }
}
