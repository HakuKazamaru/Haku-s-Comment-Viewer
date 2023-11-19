using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace HakuCommentViewer.WinForm
{
    internal static class Program
    {
        /// <summary>
        /// NLogロガー
        /// </summary>
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        /// <summary>
        /// 設定管理オブジェクト
        /// </summary>
        public static Common.Setting setting;

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            string path = "";

            // アッセンブリ情報からファイルパス取得
            try
            {
                Assembly myAssembly = Assembly.GetEntryAssembly();
                if (myAssembly is not null)
                {
                    if (!string.IsNullOrWhiteSpace(myAssembly.Location))
                    {
                        path = Path.Combine(Path.GetDirectoryName(myAssembly.Location), "appsettings.json");
                    }
                    else
                    {
                        myAssembly = Assembly.GetExecutingAssembly();
                        if (!string.IsNullOrWhiteSpace(myAssembly.Location))
                        {
                            path = Path.Combine(Path.GetDirectoryName(myAssembly.Location), "appsettings.json");
                        }
                        else
                        {
                            if (!string.IsNullOrWhiteSpace(Application.ExecutablePath))
                            {
                                path = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "appsettings.json");
                            }
                            else
                            {
                                MessageBox.Show(
                                    "アッセンブリファイルパスの取得に失敗しました。",
                                    "エラー",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Error,
                                    MessageBoxDefaultButton.Button1);
                                return;
                            }
                        }
                    }
                }
                else
                {
                    MessageBox.Show(
                        "アッセンブリエントリー情報の取得に失敗しました。",
                        "エラー",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error,
                        MessageBoxDefaultButton.Button1);
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    string.Format("アッセンブリ情報の取得に失敗しました。\r\nエラー:{0}", ex.Message),
                    "エラー",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error,
                    MessageBoxDefaultButton.Button1);
                return;
            }

            // アプリケーション設定の読み込み
            try
            {
                setting = new Common.Setting(path);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    string.Format("アプリケーション設定の読み込みに失敗しました。\r\nエラー:{0}\r\n設定ファイル:\r\n{1}", ex.Message, path),
                    "エラー",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error,
                    MessageBoxDefaultButton.Button1);
                return;
            }

            // NLogの設定反映
            NLog.LogManager.Configuration = setting.GetNLogSetting();

            logger.Info("==============================  Start   ==============================");

            try
            {
                using (var tokenSource = new CancellationTokenSource())
                {
                    CancellationToken token = tokenSource.Token;

                    using (var server = Task.Run(() => Common.WebServer.StartServerAsync(token), tokenSource.Token))
                    {
                        bool testmode = false;
                        // To customize application configuration such as set high DPI settings or default font,
                        // see https://aka.ms/applicationconfiguration.

                        ApplicationConfiguration.Initialize();

                        if (args is not null)
                        {
                            foreach (var arg in args)
                            {
                                if (arg.IndexOf("test") > -1)
                                {
                                    logger.Info("テストモードで起動します。");
                                    testmode = true;
                                    break;
                                }
                            }
                        }

                        if (testmode) Application.Run(new TestForm());
                        else Application.Run(new MainForm());

                        tokenSource.Cancel();

                        try
                        {
                            server.Wait();
                        }
                        catch (Exception ex)
                        {
                            logger.Info(ex, "WEBサーバーの終了でエラーが発生しました。メッセージ:{0}", ex.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "予期せぬエラーが発生しました。エラー:{0}", ex.Message);
            }

            logger.Info("==============================   End    ==============================");
        }
    }
}