using HakuCommentViewer.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.NetworkInformation;

namespace HakuCommentViewer.WebServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UtilController
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
        public UtilController(ILogger<UserInfoController> logger, HcvDbContext context)
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
        public async Task<IEnumerable<string>> IpList()
        {
            List<string> returnVal = new List<string>();
            _logger.LogDebug("==============================  Start   ==============================");

            var heserver = NetworkInterface.GetAllNetworkInterfaces();
            foreach (var host in heserver)
            {
                foreach (var ip in host.GetIPProperties().UnicastAddresses)
                {
                    string addr_str = ip.Address.ToString();
                    _logger.LogDebug($"IP:{addr_str}");
                    if (addr_str.IndexOf("127.0.0.1")<0 && addr_str.IndexOf("::1") < 0)
                    {
                        returnVal.Add(addr_str);
                    }
                }
            }

            _logger.LogDebug("==============================   End    ==============================");
            return returnVal.ToArray();
        }
    }
}
