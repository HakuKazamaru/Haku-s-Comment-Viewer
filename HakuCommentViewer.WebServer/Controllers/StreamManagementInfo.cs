using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using HakuCommentViewer.Common;
using HakuCommentViewer.Common.Models;
using HakuCommentViewer.WebClient.Shared;
using HakuCommentViewer.WebServer.Queues;

using NicoNico = HakuCommentViewer.Plugin.Comment.NicoNico;
using YouTube = HakuCommentViewer.Plugin.Comment.YouTube;

namespace HakuCommentViewer.WebServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class StreamManagementInfoController : Controller
    {
        /// <summary>
        /// ロガー
        /// </summary>
        private readonly ILogger<StreamManagementInfoController> _logger;

        /// <summary>
        /// DBコンテキスト
        /// </summary>
        private readonly HcvDbContext _context;

        /// <summary>
        /// バックグラウンドタスクキュー
        /// </summary>
        private readonly IBackgroundTaskQueue _queue;

        /// <summary>
        /// コンストラクター
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="context"></param>
        public StreamManagementInfoController(ILogger<StreamManagementInfoController> logger, HcvDbContext context, IBackgroundTaskQueue queue)
        {
            this._context = context;
            this._logger = logger;
            this._queue = queue;
        }

        /// <summary>
        /// 配信情報一覧取得処理
        /// GET: StreamInfo
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<IEnumerable<StreamManagementInfo>> List()
        {
            _logger.LogDebug("==============================   Call   ==============================");
            var streamManagementInfos = await this._context.StreamManagementInfos.ToArrayAsync();
            return streamManagementInfos;
        }

        /// <summary>
        /// 配信情報取得処理
        /// GET: StreamInfo/Get/5 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("[action]")]
        public async Task<StreamManagementInfo> Get(string id)
        {
            _logger.LogDebug("==============================  Start   ==============================");

            var streamManagementInfo = await _context.StreamManagementInfos
                .FirstOrDefaultAsync(m => m.StreamManagementId == id);
            _logger.LogDebug("取得結果:{0}", streamManagementInfo is not null ? "あり" : "なし、もしくは複数あり");

            _logger.LogDebug("==============================   End    ==============================");
            return streamManagementInfo;
        }

        // POST: StreamInfoes/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        public async Task<StreamManagementInfo> Create(StreamManagementInfo streamManagementInfo)
        {
            _logger.LogDebug("==============================  Start   ==============================");
            StreamManagementInfo returnVal = new StreamManagementInfo();

            _logger.LogDebug("・処理データ情報");
            _logger.LogDebug("-------------------------------------------------------------------------");
            _logger.LogDebug("配信管理ID                          :{0}", streamManagementInfo.StreamManagementId);
            _logger.LogDebug("配信管理番号(画面用)                :{0}", streamManagementInfo.StreamNo);
            _logger.LogDebug("配信ID                              :{0}", streamManagementInfo.StreamId);
            _logger.LogDebug("接続済みフラグ                      :{0}", streamManagementInfo.IsConnected);
            _logger.LogDebug("コメントジェネレーター機能利用フラグ:{0}", streamManagementInfo.UseCommentGenerator);
            _logger.LogDebug("読上げ機能利用フラグ                :{0}", streamManagementInfo.UseNarrator);
            _logger.LogDebug("備考                                :{0}", streamManagementInfo.Note);

            this._context.Add(streamManagementInfo);
            await this._context.SaveChangesAsync();

            returnVal.StreamManagementId = streamManagementInfo.StreamManagementId;

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
        public async Task<StreamManagementInfo> Update(StreamManagementInfo streamManagementInfo)
        {
            _logger.LogDebug("==============================  Start   ==============================");
            StreamManagementInfo returnVal = new StreamManagementInfo();

            _logger.LogDebug("・処理データ情報");
            _logger.LogDebug("-------------------------------------------------------------------------");
            _logger.LogDebug("配信管理ID                          :{0}", streamManagementInfo.StreamManagementId);
            _logger.LogDebug("配信管理番号(画面用)                :{0}", streamManagementInfo.StreamNo);
            _logger.LogDebug("配信ID                              :{0}", streamManagementInfo.StreamId);
            _logger.LogDebug("接続済みフラグ                      :{0}", streamManagementInfo.IsConnected);
            _logger.LogDebug("コメントジェネレーター機能利用フラグ:{0}", streamManagementInfo.UseCommentGenerator);
            _logger.LogDebug("読上げ機能利用フラグ                :{0}", streamManagementInfo.UseNarrator);
            _logger.LogDebug("備考                                :{0}", streamManagementInfo.Note);

            try
            {
                this._context.Update(streamManagementInfo);
                await this._context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (!StreamInfoExists(streamManagementInfo.StreamManagementId))
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
        public async Task<StreamManagementInfo> Delete(StreamManagementInfo streamManagementInfo)
        {
            _logger.LogDebug("==============================  Start   ==============================");
            StreamManagementInfo returnVal = new StreamManagementInfo();

            _logger.LogDebug("・処理データ情報");
            _logger.LogDebug("-------------------------------------------------------------------------");
            _logger.LogDebug("配信ID:{0}", streamManagementInfo.StreamManagementId);

            if (streamManagementInfo.StreamManagementId == null || _context.StreamManagementInfos == null)
            {
                returnVal = null;
            }
            else
            {
                var streamInfo = await this._context.StreamManagementInfos
                    .FirstOrDefaultAsync(m => m.StreamManagementId == streamManagementInfo.StreamManagementId);
                if (streamInfo == null)
                {
                    returnVal = null;
                }
                else
                {
                    returnVal = streamInfo;
                    this._context.StreamManagementInfos.Remove(streamInfo);
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
            return (this._context.StreamManagementInfos?.Any(e => e.StreamManagementId == id)).GetValueOrDefault();
        }
    }
}
