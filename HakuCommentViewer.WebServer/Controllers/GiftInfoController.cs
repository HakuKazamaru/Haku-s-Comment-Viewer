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
        /// ���K�[
        /// </summary>
        private readonly ILogger<GiftInfoController> _logger;

        /// <summary>
        /// DB�R���e�L�X�g
        /// </summary>
        private readonly HcvDbContext _context;

        /// <summary>
        /// �R���X�g���N�^�[
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="context"></param>
        public GiftInfoController(ILogger<GiftInfoController> logger, HcvDbContext context)
        {
            this._context = context;
            this._logger = logger;
        }

        /// <summary>
        /// �M�t�g�ꗗ�擾����
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
        /// �M�t�g�擾����
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
            _logger.LogDebug("�擾����:{0}", commentInfos is not null ? "����" : "�Ȃ��A�������͕�������");

            _logger.LogDebug("==============================   End    ==============================");
            return commentInfos;
        }

        /// <summary>
        /// �z�MID�Ō���
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

            _logger.LogDebug("�擾����:{0}", giftInfoList is not null ? $"����{giftInfoList.Length}" : "�Ȃ�");
            foreach (var giftInfo in giftInfoList.Select((Value, Index) => (Value, Index)))
            {
                _logger.LogTrace("[{0}]�E���[�U�[���", giftInfo.Index);
                _logger.LogTrace("[{0}]----------------------------------------------------------------------", giftInfo.Index);
                _logger.LogTrace("[{0}]�z�M���ID    �@�@:{1}", giftInfo.Index, giftInfo.Value.StreamSiteId);
                _logger.LogTrace("[{0}]�^�C���X�^���v�@�@:{1}", giftInfo.Index, giftInfo.Value.TimeStampUSec);
                _logger.LogTrace("[{0}]�R�����g���ʗp  �@:{1}", giftInfo.Index, giftInfo.Value.CommentId);
                _logger.LogTrace("[{0}]�z�M�T�C�gID    �@:{1}", giftInfo.Index, giftInfo.Value.StreamSiteId);
                _logger.LogTrace("[{0}]�z�M�Ǘ��ԍ�      :{1}", giftInfo.Index, giftInfo.Value.StreamManagementId);
                _logger.LogTrace("[{0}]���[�U�[ID      �@:{1}", giftInfo.Index, giftInfo.Value.UserId);
                _logger.LogTrace("[{0}]���[�U�[��    �@�@:{1}", giftInfo.Index, giftInfo.Value.UserName);
                _logger.LogTrace("[{0}]�X�p���E�M�t�g���:{1}", giftInfo.Index, giftInfo.Value.GiftType);
                _logger.LogTrace("[{0}]�X�p���E�M�t�g���z:{1}", giftInfo.Index, giftInfo.Value.GiftValue);
                _logger.LogTrace("[{0}]�R�����g�@�@�@�@�@:{1}", giftInfo.Index, giftInfo.Value.CommentText);
                _logger.LogTrace("[{0}]----------------------------------------------------------------------", giftInfo.Index);
            }

            _logger.LogDebug("==============================   End    ==============================");
            return returnVal is null ? null : giftInfoList.ToArray();
        }


        /// <summary>
        /// �M�t�g�o�^����
        /// </summary>
        /// <param name="commentInfo"></param>
        [HttpPost]
        public async Task<GiftInfo> Create(GiftInfo commentInfo)
        {
            _logger.LogDebug("==============================  Start   ==============================");
            GiftInfo returnVal = new GiftInfo();

            _logger.LogDebug("�E�����f�[�^���");
            _logger.LogDebug("-------------------------------------------------------------------------");
            _logger.LogDebug("�^�C���X�^���v:{0}", commentInfo.TimeStampUSec);
            _logger.LogDebug("�R�����gID    :{0}", commentInfo.CommentId);
            _logger.LogDebug("�R�����g      :{0}", commentInfo.CommentText);
            _logger.LogDebug("�z�M�Ǘ�ID    :{0}", commentInfo.StreamManagementId);
            _logger.LogDebug("�z�M�T�C�gID  :{0}", commentInfo.StreamSiteId);
            _logger.LogDebug("���[�U�[ID    :{0}", commentInfo.UserId);
            _logger.LogDebug("���[�U�[��    :{0}", commentInfo.UserName);
            _logger.LogDebug("�M�t�g���    :{0}", commentInfo.GiftType);
            _logger.LogDebug("�M�t�g���z    :{0}", commentInfo.GiftValue);

            this._context.Add(commentInfo);
            await this._context.SaveChangesAsync();

            returnVal.CommentId = commentInfo.CommentId;

            _logger.LogDebug("==============================   End    ==============================");
            return returnVal;
        }

    }
}