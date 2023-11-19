using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using HakuCommentViewer.Common;

namespace HakuCommentViewer.WinForm
{
    public partial class MainForm : Form
    {
        public enum DWMWINDOWATTRIBUTE
        {
            DWMWA_WINDOW_CORNER_PREFERENCE = 33
        }

        public enum DWM_WINDOW_CORNER_PREFERENCE
        {
            DWMWCP_DEFAULT = 0,
            DWMWCP_DONOTROUND = 1,
            DWMWCP_ROUND = 2,
            DWMWCP_ROUNDSMALL = 3
        }

        [DllImport("dwmapi.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
        internal static extern void DwmSetWindowAttribute(IntPtr hwnd,
                                                   DWMWINDOWATTRIBUTE attribute,
                                                   ref DWM_WINDOW_CORNER_PREFERENCE pvAttribute,
                                                   uint cbAttribute);

        /// <summary>
        /// NLogロガー
        /// </summary>
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private BootForm bootForm = new BootForm();

        public bool bootFinished = false;
        public bool exitFlag = false;

        public MainForm()
        {
            logger.Trace("==============================  Start   ==============================");

            InitializeComponent();

            var attribute = DWMWINDOWATTRIBUTE.DWMWA_WINDOW_CORNER_PREFERENCE;
            var preference = DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_ROUND;
            DwmSetWindowAttribute(this.Handle, attribute, ref preference, sizeof(uint));

            // WEBサーバーモジュール起動確認
            if (!WebServer.CheckRunningProcess())
            {
                // アプリケーション配置先取得
                string exePath = Path.Combine(WebServer.ServerWorkPath, WebServer.ServerExeName);
                string msg = "WEBサーバーモジュールの起動に失敗しました。\r\n";
                msg += "起動パス:" + exePath;
                MessageBox.Show(msg, "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                throw new Exception(msg);
            }

            this.WindowState = FormWindowState.Minimized;
            this.wv2Main.CreationProperties = new Microsoft.Web.WebView2.WinForms.CoreWebView2CreationProperties
            {
                UserDataFolder = Path.GetDirectoryName(Program.setting.UserConfigPath)
            };
            this.wv2Main.EnsureCoreWebView2Async();
            this.wv2Main.Source = new Uri(Program.setting.WebApiServerUrl.Replace("0.0.0.0", "localhost"));

            bootForm.Show();

            logger.Trace("==============================   End    ==============================");
        }

        /// <summary>
        /// フォーム読み込み時
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainForm_Load(object sender, EventArgs e)
        {
            if (!bootFinished)
            {
                this.Hide();
            }
        }

        /// <summary>
        /// WEBサーバープロセス監視
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tmrWebSrvCheck_Tick(object sender, EventArgs e)
        {
            if (!WebServer.CheckRunningProcess() && !exitFlag)
            {
                exitFlag = true;
                MessageBox.Show("内部サーバーが終了しました。", "情報", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1);
                this.Close();
            }
        }

        /// <summary>
        /// 読み込み完了時
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void wv2Main_NavigationCompleted(object sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs e)
        {
            if (!bootFinished)
            {
                bootForm.Close();
                this.Show();
                this.WindowState = FormWindowState.Normal;
                this.tmrWebSrvCheck.Start();
                bootFinished = true;
            }
        }

    }
}
