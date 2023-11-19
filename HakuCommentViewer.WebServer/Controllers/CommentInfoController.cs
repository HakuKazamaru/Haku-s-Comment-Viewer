using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using HakuCommentViewer.Common;
using HakuCommentViewer.Common.Models;

namespace HakuCommentViewer.WebServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CommentInfoController : ControllerBase
    {
        /// <summary>
        /// ロガー
        /// </summary>
        private readonly ILogger<CommentInfoController> _logger;

        /// <summary>
        /// DBコンテキスト
        /// </summary>
        private readonly HcvDbContext _context;

        /// <summary>
        /// コンストラクター
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="context"></param>
        public CommentInfoController(ILogger<CommentInfoController> logger, HcvDbContext context)
        {
            this._context = context;
            this._logger = logger;
        }

        /// <summary>
        /// コメント一覧取得処理
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<IEnumerable<CommentInfo>> List()
        {
            _logger.LogDebug("==============================   Call   ==============================");
            var commentInfos = await this._context.CommentInfos.ToArrayAsync();
            return commentInfos;
        }

        /// <summary>
        /// コメント取得処理
        /// </summary>
        /// <param name="timeStampUSec"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("[action]")]
        public async Task<CommentInfo> Get([FromQuery] string streamInfoId, [FromQuery] string timeStampUSec, [FromQuery] string commentId)
        {
            _logger.LogDebug("==============================  Start   ==============================");

            var commentInfos = await this._context.CommentInfos
                .SingleOrDefaultAsync(b => b.StreamInfoId.Equals(streamInfoId) && b.TimeStampUSec.Equals(timeStampUSec) && b.CommentId.Equals(commentId));
            _logger.LogDebug("取得結果:{0}", commentInfos is not null ? "あり" : "なし、もしくは複数あり");

            _logger.LogDebug("==============================   End    ==============================");
            return commentInfos;
        }

        /// <summary>
        /// コメント登録処理
        /// </summary>
        /// <param name="commentInfo"></param>
        [HttpPost]
        public async Task<CommentInfo> Create(CommentInfo commentInfo)
        {
            _logger.LogDebug("==============================  Start   ==============================");
            CommentInfo returnVal = new CommentInfo();

            _logger.LogDebug("・処理データ情報");
            _logger.LogDebug("-------------------------------------------------------------------------");
            _logger.LogDebug("タイムスタンプ:{0}", commentInfo.TimeStampUSec);
            _logger.LogDebug("コメントID    :{0}", commentInfo.CommentId);
            _logger.LogDebug("コメント      :{0}", commentInfo.CommentText);
            _logger.LogDebug("配信管理ID    :{0}", commentInfo.StreamManagementId);
            _logger.LogDebug("配信サイトID  :{0}", commentInfo.StreamSiteId);
            _logger.LogDebug("ユーザーID    :{0}", commentInfo.UserId);
            _logger.LogDebug("ユーザー名    :{0}", commentInfo.UserName);

            this._context.Add(commentInfo);
            await this._context.SaveChangesAsync();

            returnVal.CommentId = commentInfo.CommentId;

            _logger.LogDebug("==============================   End    ==============================");
            return returnVal;
        }

    }
}