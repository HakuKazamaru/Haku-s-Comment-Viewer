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
        /// ���K�[
        /// </summary>
        private readonly ILogger<ExitController> _logger;

        /// <summary>
        /// DB�R���e�L�X�g
        /// </summary>
        private readonly HcvDbContext _context;

        /// <summary>
        /// �R���X�g���N�^�[
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="context"></param>
        public ExitController(ILogger<ExitController> logger, HcvDbContext context)
        {
            this._context = context;
            this._logger = logger;
        }

        /// <summary>
        /// �T�[�o�[�I��
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