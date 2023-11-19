using System.Net.WebSockets;
using System.Text;
using System.Text.Json.Nodes;

using NLog.Web;

using HakuCommentViewer.Plugin;
using HakuCommentViewer.Plugin.Comment.NicoNico;

namespace NC_Test
{
    /// <summary>
    /// メインクラス
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// ロガー
        /// </summary>
        private static NLog.Logger logger;

        /// <summary>
        /// 並列処理停止フラグ
        /// </summary>
        private static bool cancelFlag = false;

        /// <summary>
        /// メインメソッド
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {
            bool result = false;
            try
            {
                // ロガーの設定読み込み
                logger = NLogBuilder.ConfigureNLog("nlog.config").GetCurrentClassLogger();
                // logger = NLog.LogManager.Setup().LoadConfigurationFromAppSettings();
                logger.Info("========== App Start! ==================================================");

                // スクレイピング先URL
                string url = "https://live.nicovideo.jp/watch/lv343218134?ref=RankingPage-OfficialAndChannelProgramListSection-ProgramCard&provider_type=official_and_channel";

                using (CancellationTokenSource cancellationTokenSource = new CancellationTokenSource())
                {
                    var commentFunc = new Comment(0, url);
                    using (var task = Task.Run(() => commentFunc.StartAsync(cancellationTokenSource.Token)))
                    {
                        task.Wait();
                        if (task.Result)
                        {
                            logger.Info("終了するにはエスケープキーを押してください。");
                            using (var checkKeyStatu = Task.Run(() => CheckEscapeKey()))
                            {
                                while (!cancelFlag)
                                {
                                    Thread.Sleep(250);
                                }
                                cancellationTokenSource.Cancel();
                                checkKeyStatu.Wait();
                            }
                        }
                        else
                        {
                            logger.Error("コメント取得に失敗しました。");
                        }
                    }
                }
                logger.Info("========== App End!　 ==================================================");

            }
            catch (Exception ex)
            {
                Console.WriteLine("ロガーの設定の読み込みに失敗しました。");
                Console.WriteLine("エラーメッセージ：" + ex.Message);
            }
        }

        /// <summary>
        /// エスケープキー監視処理（処理中断用）
        /// </summary>
        /// <returns></returns>
        private static async Task CheckEscapeKey()
        {
            logger.Info("========== Func Start! ==================================================");
            while (true)
            {
                ConsoleKeyInfo consoleKeyInfo = Console.ReadKey();

                if (consoleKeyInfo.Key == ConsoleKey.Escape)
                {
                    logger.Info("エスケープキーが押下されました。");
                    cancelFlag = true;
                    break;
                }

                Thread.Sleep(250);
            }
            logger.Info("========== Func End!　 ==================================================");
        }

    }
}


