using HakuCommentViewer.Common.Models;
using HakuCommentViewer.Plugin.Models;

namespace HakuCommentViewer.Plugin.Interface
{
    /// <summary>
    /// コメント取得/送信プラグインインターフェイス
    /// </summary>
    public interface IComment : IPlugin
    {
        /// <summary>
        /// 配信管理情報
        /// </summary>
        public StreamManagementInfo StreamManagementInfo { get; set; }

        /// <summary>
        /// 配信管理情報
        /// </summary>
        public StreamInfo StreamInfo { get; }

        /// <summary>
        /// コメント受信イベントハンドラ
        /// </summary>
        public event EventHandler<GetNewCommentArgs> GetCommentHandler;

        /// <summary>
        /// 配信情報取得処理
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task<bool> GetStreamInfo(CancellationToken cancellationToken);

        /// <summary>
        /// コメント取得開始
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task<bool> StartAsync(CancellationToken cancellationToken);

        /// <summary>
        /// コメント取得停止
        /// </summary>
        /// <returns></returns>
        public Task StopAsync();

    }
}