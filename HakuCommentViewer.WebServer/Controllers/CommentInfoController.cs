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
        /// ���K�[
        /// </summary>
        private readonly ILogger<CommentInfoController> _logger;

        /// <summary>
        /// DB�R���e�L�X�g
        /// </summary>
        private readonly HcvDbContext _context;

        /// <summary>
        /// �R���X�g���N�^�[
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="context"></param>
        public CommentInfoController(ILogger<CommentInfoController> logger, HcvDbContext context)
        {
            this._context = context;
            this._logger = logger;
        }

        /// <summary>
        /// �R�����g�ꗗ�擾����
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
        /// �R�����g�擾����
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
            _logger.LogDebug("�擾����:{0}", commentInfos is not null ? "����" : "�Ȃ��A�������͕�������");

            _logger.LogDebug("==============================   End    ==============================");
            return commentInfos;
        }

        /// <summary>
        /// �R�����g�o�^����
        /// </summary>
        /// <param name="commentInfo"></param>
        [HttpPost]
        public async Task<CommentInfo> Create(CommentInfo commentInfo)
        {
            _logger.LogDebug("==============================  Start   ==============================");
            CommentInfo returnVal = new CommentInfo();

            _logger.LogDebug("�E�����f�[�^���");
            _logger.LogDebug("-------------------------------------------------------------------------");
            _logger.LogDebug("�^�C���X�^���v:{0}", commentInfo.TimeStampUSec);
            _logger.LogDebug("�R�����gID    :{0}", commentInfo.CommentId);
            _logger.LogDebug("�R�����g      :{0}", commentInfo.CommentText);
            _logger.LogDebug("�z�M�Ǘ�ID    :{0}", commentInfo.StreamManagementId);
            _logger.LogDebug("�z�M�T�C�gID  :{0}", commentInfo.StreamSiteId);
            _logger.LogDebug("���[�U�[ID    :{0}", commentInfo.UserId);
            _logger.LogDebug("���[�U�[��    :{0}", commentInfo.UserName);

            this._context.Add(commentInfo);
            await this._context.SaveChangesAsync();

            returnVal.CommentId = commentInfo.CommentId;

            _logger.LogDebug("==============================   End    ==============================");
            return returnVal;
        }

    }
}