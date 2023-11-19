using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;

using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Jint.Parser.Ast;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using NLog.LayoutRenderers;
using NLog.Web;

using HakuCommentViewer.Plugin;
using HakuCommentViewer.Plugin.Comment.YouTube;

namespace YT_Test
{
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
            // ロガーの設定読み込み
            try
            {
                string chatId = "";

                logger = NLogBuilder.ConfigureNLog("nlog.config").GetCurrentClassLogger();
                // logger = NLog.LogManager.Setup().LoadConfigurationFromAppSettings();
                logger.Info("========== App Start! ==================================================");

                // スクレイピング先URL
                string url = "https://www.youtube.com/watch?v=79XaA_4CYj8";

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

