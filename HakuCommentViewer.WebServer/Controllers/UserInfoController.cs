using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using HakuCommentViewer.Common;
using HakuCommentViewer.Common.Models;
using System.Linq.Dynamic.Core;
using System.Net.NetworkInformation;

namespace HakuCommentViewer.WebServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UserInfoController : Controller
    {
        /// <summary>
        /// ロガー
        /// </summary>
        private readonly ILogger<UserInfoController> _logger;

        /// <summary>
        /// DBコンテキスト
        /// </summary>
        private readonly HcvDbContext _context;

        /// <summary>
        /// コンストラクター
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="context"></param>
        public UserInfoController(ILogger<UserInfoController> logger, HcvDbContext context)
        {
            this._context = context;
            this._logger = logger;
        }

        /// <summary>
        /// ユーザー情報一覧取得処理
        /// GET: StreamInfo
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<IEnumerable<UserInfo>> List()
        {
            _logger.LogDebug("==============================   Call   ==============================");
            var userInfos = await this._context.UserInfos.ToArrayAsync();
            return userInfos;
        }

        /// <summary>
        /// ユーザー情報取得処理
        /// GET: StreamInfo/Get?streamSiteId=hoge&userId=5 
        /// </summary>
        /// <param name="streamSiteId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        [HttpGet("[action]")]
        public async Task<UserInfo> Get([FromQuery] string streamSiteId, [FromQuery] string userId)
        {
            _logger.LogDebug("==============================  Start   ==============================");

            var userInfo = await _context.UserInfos
                .SingleOrDefaultAsync(m => m.StreamSiteId == streamSiteId && m.UserId == userId);
            _logger.LogDebug("取得結果:{0}", userInfo is not null ? "あり" : "なし、もしくは複数あり");

            _logger.LogDebug("==============================   End    ==============================");
            return userInfo;
        }

        /// <summary>
        /// ユーザー情報取得処理
        /// GET: StreamInfo/GetByName?streamSiteId=hoge&userName=hoge 
        /// </summary>
        /// <param name="streamSiteId"></param>
        /// <param name="userName"></param>
        /// <returns></returns>
        [HttpGet("[action]")]
        public async Task<UserInfo> GetByName([FromQuery] string streamSiteId, [FromQuery] string userName)
        {
            _logger.LogDebug("==============================  Start   ==============================");

            var userInfo = await _context.UserInfos
                .FirstOrDefaultAsync(m => m.StreamSiteId == streamSiteId && m.UserName == userName);
            _logger.LogDebug("取得結果:{0}", userInfo is not null ? "あり" : "なし、もしくは複数あり");

            _logger.LogDebug("==============================   End    ==============================");
            return userInfo;
        }

        /// <summary>
        /// 配信IDで検索
        /// </summary>
        /// <param name="streamSiteId"></param>
        /// <param name="streamInfoId"></param>
        /// <returns></returns>
        [HttpGet("[action]")]
        public async Task<UserInfo[]?> GetByStreamId([FromQuery] string streamSiteId, [FromQuery] string streamInfoId)
        {
            List<UserInfo> returnVal = new List<UserInfo>();
            _logger.LogDebug("==============================  Start   ==============================");

            var userInfoList = await _context.UserInfos
                .Where(m => m.StreamSiteId == streamSiteId)
                .Join(
                    _context.CommentInfos.Where(w => w.StreamSiteId == streamSiteId && w.StreamInfoId == streamInfoId),
                    userInfo => new { userInfo.StreamSiteId, userInfo.UserId },
                    commentInfo => new { commentInfo.StreamSiteId, commentInfo.UserId },
                    (userInfo, commentInfo) => new
                    {
                        StreamSiteId = userInfo.StreamSiteId,
                        UserId = userInfo.UserId,
                        UserName = userInfo.UserName,
                        HandleName = userInfo.HandleName,
                        IconPath = userInfo.IconPath,
                        CommentCount = userInfo.CommentCount,
                        GiftCount = userInfo.GiftCount,
                        FastCommentDateTime = userInfo.FastCommentDateTime,
                        LastCommentDateTime = userInfo.LastCommentDateTime,
                        Note = userInfo.Note,
                    }
            ).Distinct()
            .ToArrayAsync();

            _logger.LogDebug("取得結果:{0}", userInfoList is not null ? $"あり{userInfoList.Length}" : "なし");
            foreach (var userInfo in userInfoList.Select((Value, Index) => (Value, Index)))
            {
                UserInfo tmpUserInfo = new UserInfo();

                _logger.LogTrace("[{0}]・ユーザー情報", userInfo.Index);
                _logger.LogTrace("[{0}]----------------------------------------------------------------------", userInfo.Index);
                _logger.LogTrace("[{0}]配信サイトID　　:{1}", userInfo.Index, userInfo.Value.StreamSiteId);
                _logger.LogTrace("[{0}]ユーザーID　　　:{1}", userInfo.Index, userInfo.Value.UserId);
                _logger.LogTrace("[{0}]ユーザー名　　　:{1}", userInfo.Index, userInfo.Value.UserName);
                _logger.LogTrace("[{0}]ハンドルネーム　:{1}", userInfo.Index, userInfo.Value.HandleName);
                _logger.LogTrace("[{0}]アイコン配置URL :{1}", userInfo.Index, userInfo.Value.IconPath);
                _logger.LogTrace("[{0}]累計コメント数　:{1}", userInfo.Index, userInfo.Value.CommentCount);
                _logger.LogTrace("[{0}]累計ギフト数　　:{1}", userInfo.Index, userInfo.Value.GiftCount);
                _logger.LogTrace("[{0}]初回コメント日時:{1}", userInfo.Index, userInfo.Value.FastCommentDateTime);
                _logger.LogTrace("[{0}]最終コメント日時:{1}", userInfo.Index, userInfo.Value.LastCommentDateTime);
                _logger.LogTrace("[{0}]備考　　　　　　:{1}", userInfo.Index, userInfo.Value.Note);
                _logger.LogTrace("[{0}]----------------------------------------------------------------------", userInfo.Index);

                tmpUserInfo.StreamSiteId = userInfo.Value.StreamSiteId;
                tmpUserInfo.UserId = userInfo.Value.UserId;
                tmpUserInfo.UserName = userInfo.Value.UserName;
                tmpUserInfo.HandleName = userInfo.Value.HandleName;
                tmpUserInfo.IconPath = userInfo.Value.IconPath;
                tmpUserInfo.CommentCount = userInfo.Value.CommentCount;
                tmpUserInfo.GiftCount = userInfo.Value.GiftCount;
                tmpUserInfo.FastCommentDateTime = userInfo.Value.FastCommentDateTime;
                tmpUserInfo.LastCommentDateTime = userInfo.Value.LastCommentDateTime;
                tmpUserInfo.Note = userInfo.Value.Note;

                returnVal.Add(tmpUserInfo);
            }

            _logger.LogDebug("==============================   End    ==============================");
            return returnVal is null ? null : returnVal.ToArray();
        }

        /// <summary>
        /// ユーザー登録処理
        /// POST: StreamInfoes/Create
        /// </summary>
        /// <param name="userInfo"></param>
        /// <returns></returns>
        [HttpPost("[action]")]
        public async Task<UserInfo> Create(UserInfo userInfo)
        {
            _logger.LogDebug("==============================  Start   ==============================");
            UserInfo returnVal = new UserInfo();

            _logger.LogDebug("・処理データ情報");
            _logger.LogDebug("-------------------------------------------------------------------------");
            _logger.LogDebug("配信サイトID:{0}", userInfo.StreamSiteId);
            _logger.LogDebug("ユーザーID  :{0}", userInfo.UserId);
            _logger.LogDebug("ユーザー名  :{0}", userInfo.UserName);
            _logger.LogDebug("アイコンURL :{0}", userInfo.IconPath);
            _logger.LogDebug("備考        :{0}", userInfo.Note);

            this._context.Add(userInfo);
            await this._context.SaveChangesAsync();

            returnVal.UserId = userInfo.UserId;

            _logger.LogDebug("==============================   End    ==============================");
            return returnVal;
        }

        /// <summary>
        /// ユーザー情報更新処理
        /// POST: StreamInfoes/Update
        /// </summary>
        /// <param name="userInfo"></param>
        /// <returns></returns>
        [HttpPost("[action]")]
        public async Task<UserInfo> Update(UserInfo userInfo)
        {
            _logger.LogDebug("==============================  Start   ==============================");
            UserInfo returnVal = new UserInfo();

            _logger.LogDebug("・処理データ情報");
            _logger.LogDebug("-------------------------------------------------------------------------");
            _logger.LogDebug("配信サイトID:{0}", userInfo.StreamSiteId);
            _logger.LogDebug("ユーザーID  :{0}", userInfo.UserId);
            _logger.LogDebug("ユーザー名  :{0}", userInfo.UserName);
            _logger.LogDebug("アイコンURL :{0}", userInfo.IconPath);
            _logger.LogDebug("備考        :{0}", userInfo.Note);

            try
            {
                this._context.Update(userInfo);
                await this._context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (!StreamInfoExists(userInfo.StreamSiteId, userInfo.UserId))
                {
                    returnVal = null;
                }
                else
                {
                    throw;
                }
            }

            _logger.LogDebug("==============================   End    ==============================");
            return returnVal;
        }

        /// <summary>
        /// 削除メソッド
        /// GET: StreamInfoes/Delete?streamSiteId=hoge&userId=5 
        /// </summary>
        /// <param name="streamSiteId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        [HttpPost("[action]")]
        public async Task<UserInfo> Delete([FromQuery] string streamSiteId, [FromQuery] string userId)
        {
            _logger.LogDebug("==============================  Start   ==============================");
            UserInfo returnVal = new UserInfo();

            _logger.LogDebug("・処理データ情報");
            _logger.LogDebug("-------------------------------------------------------------------------");
            _logger.LogDebug("配信サイトID:{0}", streamSiteId);
            _logger.LogDebug("ユーザーID  :{0}", userId);

            if (streamSiteId == null || userId == null || _context.StreamInfos == null)
            {
                returnVal = null;
            }
            else
            {
                var userInfo = await this._context.UserInfos
                    .FirstOrDefaultAsync(m => m.StreamSiteId == streamSiteId && m.UserId == userId);
                if (userInfo == null)
                {
                    returnVal = null;
                }
                else
                {
                    returnVal = userInfo;
                    this._context.UserInfos.Remove(userInfo);
                    await this._context.SaveChangesAsync();
                }
            }

            _logger.LogDebug("==============================   End    ==============================");
            return returnVal;
        }

        /// <summary>
        /// ユーザー情報データ存在確認
        /// </summary>
        /// <param name="streamSiteId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        private bool StreamInfoExists(string streamSiteId, string userId)
        {
            _logger.LogDebug("==============================   Call   ==============================");
            return (
                this._context.UserInfos?
                    .Any(e => e.StreamSiteId == streamSiteId && e.UserId == userId)
                ).GetValueOrDefault();
        }
    }
}
