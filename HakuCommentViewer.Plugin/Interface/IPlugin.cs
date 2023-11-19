using HakuCommentViewer.Common.Models;
using HakuCommentViewer.Plugin.Models;

namespace HakuCommentViewer.Plugin.Interface
{
    /// <summary>
    /// プラグイン基本インターフェース
    /// </summary>
    public interface IPlugin : IDisposable
    {
        /// <summary>
        /// プラグイン管理情報
        /// </summary>
        public PluginInfo PluginInfo { get; }

        /// <summary>
        /// プラグイン読込状態
        /// </summary>
        public bool IsLoaded { get; }

        /// <summary>
        /// プラグイン実行状態
        /// </summary>
        public bool IsRunning { get; }

        /// <summary>
        /// 例外発生イベントハンドラー
        /// </summary>
        public event EventHandler<OnExcrptionArgs> OnExcrptionHandler;

    }
}
