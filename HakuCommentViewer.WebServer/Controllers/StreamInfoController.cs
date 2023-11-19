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
    public class StreamInfoController : Controller
    {
        /// <summary>
        /// ロガー
        /// </summary>
        private readonly ILogger<StreamInfoController> _logger;

        /// <summary>
        /// DBコンテキスト
        /// </summary>
        private readonly HcvDbContext _context;

        /// <summary>
        /// コンストラクター
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="context"></param>
        public StreamInfoController(ILogger<StreamInfoController> logger, HcvDbContext context)
        {
            this._context = context;
            this._logger = logger;
        }

        /// <summary>
        /// 配信情報一覧取得処理
        /// GET: StreamInfo
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<IEnumerable<StreamInfo>> List()
        {
            _logger.LogDebug("==============================   Call   ==============================");
            var streamInfos = await this._context.StreamInfos.ToArrayAsync();
            return streamInfos;
        }

        /// <summary>
        /// 配信情報取得処理
        /// GET: StreamInfo/Get/5 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("[action]")]
        public async Task<StreamInfo> Get(string id)
        {
            _logger.LogDebug("==============================  Start   ==============================");

            var streamInfo = await _context.StreamInfos
                .FirstOrDefaultAsync(m => m.StreamId == id);
            _logger.LogDebug("取得結果:{0}", streamInfo is not null ? "あり" : "なし、もしくは複数あり");
            streamInfo = streamInfo is null ? new StreamInfo() : streamInfo;

            _logger.LogDebug("==============================   End    ==============================");
            return streamInfo;
        }

        /// <summary>
        /// 配信情報取得処理
        /// GET: StreamInfo/Get/5 
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        [HttpGet("[action]")]
        public async Task<StreamInfo> GetByUrl(string url)
        {
            _logger.LogDebug("==============================  Start   ==============================");

            var streamInfo = await _context.StreamInfos
                .FirstOrDefaultAsync(m => m.StreamUrl == url);
            _logger.LogDebug("取得結果:{0}", streamInfo is not null ? "あり" : "なし、もしくは複数あり");
            streamInfo = streamInfo is null ? new StreamInfo() : streamInfo;

            _logger.LogDebug("==============================   End    ==============================");
            return streamInfo;
        }

        // POST: StreamInfoes/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        public async Task<StreamInfo> Create(StreamInfo streamInfo)
        {
            _logger.LogDebug("==============================  Start   ==============================");
            StreamInfo returnVal = new StreamInfo();

            _logger.LogDebug("・処理データ情報");
            _logger.LogDebug("-------------------------------------------------------------------------");
            _logger.LogDebug("配信ID                    :{0}", streamInfo.StreamId);
            _logger.LogDebug("配信名                    :{0}", streamInfo.StreamName);
            _logger.LogDebug("配信URL                   :{0}", streamInfo.StreamUrl);
            _logger.LogDebug("コメント(チャット)ルームID:{0}", streamInfo.CommentRoomId);
            _logger.LogDebug("配信サイトID              :{0}", streamInfo.StreamSiteId);
            _logger.LogDebug("配信開始日時              :{0}", streamInfo.StartDateTime);
            _logger.LogDebug("配信終了日時              :{0}", streamInfo.EndDateTime);
            _logger.LogDebug("視聴者数                  :{0}", streamInfo.ViewerCount);
            _logger.LogDebug("コメント数                :{0}", streamInfo.CommentCount);
            _logger.LogDebug("備考                      :{0}", streamInfo.Note);

            this._context.Add(streamInfo);
            await this._context.SaveChangesAsync();

            returnVal.StreamId = streamInfo.StreamId;

            _logger.LogDebug("==============================   End    ==============================");
            return returnVal;
        }

        /// <summary>
        /// 配信情報更新処理
        /// POST: StreamInfoes/Update
        /// </summary>
        /// <param name="streamInfo"></param>
        /// <returns></returns>
        [HttpPost("[action]")]
        public async Task<StreamInfo> Update(StreamInfo streamInfo)
        {
            _logger.LogDebug("==============================  Start   ==============================");
            StreamInfo returnVal = new StreamInfo();

            _logger.LogDebug("・処理データ情報");
            _logger.LogDebug("-------------------------------------------------------------------------");
            _logger.LogDebug("配信ID                    :{0}", streamInfo.StreamId);
            _logger.LogDebug("配信名                    :{0}", streamInfo.StreamName);
            _logger.LogDebug("配信URL                   :{0}", streamInfo.StreamUrl);
            _logger.LogDebug("コメント(チャット)ルームID:{0}", streamInfo.CommentRoomId);
            _logger.LogDebug("配信サイトID              :{0}", streamInfo.StreamSiteId);
            _logger.LogDebug("配信開始日時              :{0}", streamInfo.StartDateTime);
            _logger.LogDebug("配信終了日時              :{0}", streamInfo.StartDateTime);
            _logger.LogDebug("視聴者数                  :{0}", streamInfo.ViewerCount);
            _logger.LogDebug("コメント数                :{0}", streamInfo.CommentCount);
            _logger.LogDebug("備考                      :{0}", streamInfo.Note);

            try
            {
                this._context.Update(streamInfo);
                await this._context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (!StreamInfoExists(streamInfo.StreamSiteId))
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
        /// GET: StreamInfoes/Delete/5
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPost("[action]")]
        public async Task<StreamInfo> Delete(string id)
        {
            _logger.LogDebug("==============================  Start   ==============================");
            StreamInfo returnVal = new StreamInfo();

            _logger.LogDebug("・処理データ情報");
            _logger.LogDebug("-------------------------------------------------------------------------");
            _logger.LogDebug("配信ID:{0}", id);

            if (id == null || _context.StreamInfos == null)
            {
                returnVal = null;
            }
            else
            {
                var streamInfo = await this._context.StreamInfos
                    .FirstOrDefaultAsync(m => m.StreamId == id);
                if (streamInfo == null)
                {
                    returnVal = null;
                }
                else
                {
                    returnVal = streamInfo;
                    this._context.StreamInfos.Remove(streamInfo);
                    await this._context.SaveChangesAsync();
                }
            }

            _logger.LogDebug("==============================   End    ==============================");
            return returnVal;
        }

        /// <summary>
        /// 配信情報データ存在確認
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private bool StreamInfoExists(string id)
        {
            return (this._context.StreamInfos?.Any(e => e.StreamId == id)).GetValueOrDefault();
        }
    }
}
