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
    public class ExitController : ControllerBase
    {
        /// <summary>
        /// ロガー
        /// </summary>
        private readonly ILogger<ExitController> _logger;

        /// <summary>
        /// DBコンテキスト
        /// </summary>
        private readonly HcvDbContext _context;

        /// <summary>
        /// コンストラクター
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="context"></param>
        public ExitController(ILogger<ExitController> logger, HcvDbContext context)
        {
            this._context = context;
            this._logger = logger;
        }

        /// <summary>
        /// サーバー終了
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResult Exit()
        {
            _logger.LogDebug("==============================   Call   ==============================");
            Environment.Exit(0);
            return Ok();
        }

    }
}