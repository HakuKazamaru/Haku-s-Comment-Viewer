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
    public class GiftInfoController : ControllerBase
    {
        /// <summary>
        /// ロガー
        /// </summary>
        private readonly ILogger<GiftInfoController> _logger;

        /// <summary>
        /// DBコンテキスト
        /// </summary>
        private readonly HcvDbContext _context;

        /// <summary>
        /// コンストラクター
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="context"></param>
        public GiftInfoController(ILogger<GiftInfoController> logger, HcvDbContext context)
        {
            this._context = context;
            this._logger = logger;
        }

        /// <summary>
        /// ギフト一覧取得処理
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<IEnumerable<GiftInfo>> List()
        {
            _logger.LogDebug("==============================   Call   ==============================");
            var commentInfos = await this._context.GiftInfos.ToArrayAsync();
            return commentInfos;
        }

        /// <summary>
        /// ギフト取得処理
        /// </summary>
        /// <param name="timeStampUSec"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("[action]")]
        public async Task<GiftInfo> Get([FromQuery] string streamInfoId, [FromQuery] string timeStampUSec, [FromQuery] string commentId)
        {
            _logger.LogDebug("==============================  Start   ==============================");

            var commentInfos = await this._context.GiftInfos
                .SingleOrDefaultAsync(b => b.StreamInfoId.Equals(streamInfoId) && b.TimeStampUSec.Equals(timeStampUSec) && b.CommentId.Equals(commentId));
            _logger.LogDebug("取得結果:{0}", commentInfos is not null ? "あり" : "なし、もしくは複数あり");

            _logger.LogDebug("==============================   End    ==============================");
            return commentInfos;
        }

        /// <summary>
        /// 配信IDで検索
        /// </summary>
        /// <param name="streamSiteId"></param>
        /// <param name="streamInfoId"></param>
        /// <returns></returns>
        [HttpGet("[action]")]
        public async Task<GiftInfo[]?> GetByStreamId([FromQuery] string streamSiteId, [FromQuery] string streamInfoId)
        {
            List<GiftInfo> returnVal = new List<GiftInfo>();
            _logger.LogDebug("==============================  Start   ==============================");

            var giftInfoList = await _context.GiftInfos
                .Where(m => m.StreamSiteId == streamSiteId && m.StreamInfoId == streamInfoId)
                .ToArrayAsync();

            _logger.LogDebug("取得結果:{0}", giftInfoList is not null ? $"あり{giftInfoList.Length}" : "なし");
            foreach (var giftInfo in giftInfoList.Select((Value, Index) => (Value, Index)))
            {
                _logger.LogTrace("[{0}]・ユーザー情報", giftInfo.Index);
                _logger.LogTrace("[{0}]----------------------------------------------------------------------", giftInfo.Index);
                _logger.LogTrace("[{0}]配信情報ID    　　:{1}", giftInfo.Index, giftInfo.Value.StreamSiteId);
                _logger.LogTrace("[{0}]タイムスタンプ　　:{1}", giftInfo.Index, giftInfo.Value.TimeStampUSec);
                _logger.LogTrace("[{0}]コメント識別用  　:{1}", giftInfo.Index, giftInfo.Value.CommentId);
                _logger.LogTrace("[{0}]配信サイトID    　:{1}", giftInfo.Index, giftInfo.Value.StreamSiteId);
                _logger.LogTrace("[{0}]配信管理番号      :{1}", giftInfo.Index, giftInfo.Value.StreamManagementId);
                _logger.LogTrace("[{0}]ユーザーID      　:{1}", giftInfo.Index, giftInfo.Value.UserId);
                _logger.LogTrace("[{0}]ユーザー名    　　:{1}", giftInfo.Index, giftInfo.Value.UserName);
                _logger.LogTrace("[{0}]スパ茶・ギフト種別:{1}", giftInfo.Index, giftInfo.Value.GiftType);
                _logger.LogTrace("[{0}]スパ茶・ギフト金額:{1}", giftInfo.Index, giftInfo.Value.GiftValue);
                _logger.LogTrace("[{0}]コメント　　　　　:{1}", giftInfo.Index, giftInfo.Value.CommentText);
                _logger.LogTrace("[{0}]----------------------------------------------------------------------", giftInfo.Index);
            }

            _logger.LogDebug("==============================   End    ==============================");
            return returnVal is null ? null : giftInfoList.ToArray();
        }


        /// <summary>
        /// ギフト登録処理
        /// </summary>
        /// <param name="commentInfo"></param>
        [HttpPost]
        public async Task<GiftInfo> Create(GiftInfo commentInfo)
        {
            _logger.LogDebug("==============================  Start   ==============================");
            GiftInfo returnVal = new GiftInfo();

            _logger.LogDebug("・処理データ情報");
            _logger.LogDebug("-------------------------------------------------------------------------");
            _logger.LogDebug("タイムスタンプ:{0}", commentInfo.TimeStampUSec);
            _logger.LogDebug("コメントID    :{0}", commentInfo.CommentId);
            _logger.LogDebug("コメント      :{0}", commentInfo.CommentText);
            _logger.LogDebug("配信管理ID    :{0}", commentInfo.StreamManagementId);
            _logger.LogDebug("配信サイトID  :{0}", commentInfo.StreamSiteId);
            _logger.LogDebug("ユーザーID    :{0}", commentInfo.UserId);
            _logger.LogDebug("ユーザー名    :{0}", commentInfo.UserName);
            _logger.LogDebug("ギフト種別    :{0}", commentInfo.GiftType);
            _logger.LogDebug("ギフト金額    :{0}", commentInfo.GiftValue);

            this._context.Add(commentInfo);
            await this._context.SaveChangesAsync();

            returnVal.CommentId = commentInfo.CommentId;

            _logger.LogDebug("==============================   End    ==============================");
            return returnVal;
        }

    }
}