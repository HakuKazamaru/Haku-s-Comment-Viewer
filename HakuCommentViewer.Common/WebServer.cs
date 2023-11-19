using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace HakuCommentViewer.Common
{
    public class WebServer
    {
        /// <summary>
        /// NLogロガー
        /// </summary>
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        /// <summary>
        /// 設定管理オブジェクト
        /// </summary>
        private static Setting setting = new Setting();

        /// <summary>
        /// WEBサーバーモジュール名
        /// </summary>
        public const string ServerExeName = "HakuCommentViewer.WebServer.exe";

        /// <summary>
        /// WEBサーバーワークディレクトリ
        /// </summary>
        public static string ServerWorkPath = Path.Combine(setting.GetAssemblyDir(), "WebServer");

        /// <summary>
        /// WEBサーバーモジュール起動処理
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public static async Task StartServerAsync(CancellationToken token)
        {
            logger.Info("==============================  Start   ==============================");

            // アプリケーション配置先取得
            string exePath = Path.Combine(ServerWorkPath, ServerExeName);

            // 処理キャンセル確認
            token.ThrowIfCancellationRequested();

            logger.Debug("ワークパス　:{0}", ServerWorkPath);
            logger.Debug("起動ファイル:{0}", exePath);

            // ファイル存在確認
            if (!File.Exists(exePath))
            {
                throw new FileNotFoundException(string.Format("WEBサーバーモジュールが見つかりません。ファイルパス:{0}", exePath));
            }

            // 事前起動確認（起動している場合は終了する）
            if (CheckRunningProcess())
            {
                KillServer();
            }

            // WEBサーバー起動
            using (var proc = new Process())
            {
                proc.StartInfo.FileName = Path.Combine(ServerWorkPath, "HakuCommentViewer.WebServer.exe");
                proc.StartInfo.CreateNoWindow = true;
                proc.StartInfo.WorkingDirectory = ServerWorkPath;

                proc.Start();
                logger.Info("WEBサーバーを開始しました。");

                while (!token.IsCancellationRequested)
                {
                    Thread.Sleep(250);
                }
                logger.Info("終了信号を受信しました。");

                if (token.IsCancellationRequested)
                {
                    proc.Kill();
                    logger.Info("WEBサーバーを終了しました。");
                }

                await proc.WaitForExitAsync();
            }

            // 処理キャンセル信号返却
            // token.ThrowIfCancellationRequested();

            logger.Info("==============================   End    ==============================");
        }

        /// <summary>
        /// WEBサーバーモジュール実行確認
        /// </summary>
        /// <returns></returns>
        public static bool CheckRunningProcess()
        {
            bool returnVal = false;
            string strExeName = Path.GetFileNameWithoutExtension(ServerExeName);
            logger.Debug("==============================  Start   ==============================");

            Process[] ps = Process.GetProcessesByName(strExeName);
            logger.Debug("検索対象:{0} 検索結果:{1}", strExeName, ps.Length);
            if (ps.Length > 0) returnVal = true;

            logger.Debug("==============================   End    ==============================");
            return returnVal;
        }

        /// <summary>
        /// WEBサーバーモジュール停止処理
        /// </summary>
        /// <returns></returns>
        public static bool KillServer()
        {
            bool returnVal = false;
            string strExeName = Path.GetFileNameWithoutExtension(ServerExeName);
            logger.Debug("==============================  Start   ==============================");

            Process[] ps = Process.GetProcessesByName(strExeName);
            logger.Debug("検索対象:{0} 検索結果:{1}", strExeName, ps.Length);
            if (ps.Length > 0)
            {
                logger.Info("起動数:{0}", ps.Length);
                foreach (var p in ps.Select((Value, Index) => (Value, Index)))
                {
                    p.Value.Kill();
                    logger.Info("[{0}]タスクキルしました。停止対象　PID:{1}", p.Index, p.Value.Id);
                }
                returnVal = true;
            }

            logger.Debug("==============================   End    ==============================");
            return returnVal;
        }
    }
}
