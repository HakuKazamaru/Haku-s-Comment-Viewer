using Microsoft.Extensions.Configuration;
using NLog.Config;
using NLog.Extensions.Logging;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using static System.Net.Mime.MediaTypeNames;

namespace HakuCommentViewer.Common
{
    public class Setting
    {
        /// <summary>
        /// NLogロガー
        /// </summary>
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        /// <summary>
        /// アプリ名
        /// </summary>
        private static string appName = Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().GetName().Name);

        /// <summary>
        /// アプリケーション設定ファイル管理用
        /// </summary>
        public IConfigurationRoot? AppConfig { get; set; }

        /// <summary>
        /// NLog設定
        /// </summary>
        public LoggingConfiguration NLogConfiguration { get; set; }

        /// <summary>
        /// アプリケーション設定ファイル保存パス
        /// 　(ファイル名付きフルパス)
        /// </summary>
        public string AppConfigPath { get; set; }

        /// <summary>
        /// ユーザー設定ファイル保存パス
        /// 　(ファイル名付きフルパス)
        /// </summary>
        public string UserConfigPath { get; set; }

        /// <summary>
        /// WEB APIサーバーバインドURL
        /// </summary>
        public string WebApiServerUrl { get; set; }

        /// <summary>
        /// コンストラクター
        /// </summary>
        public Setting() : this(Directory.GetCurrentDirectory())
        {
            logger.Trace("==============================   Call   ==============================");
        }

        /// <summary>
        /// コンストラクター
        /// </summary>
        /// <param name="settingDir"></param>
        public Setting(string settingDir)
        {
            logger.Trace("==============================  Start   ==============================");

            try
            {
                if (CheckAppSettingFileExists(Path.Combine(settingDir, "appsettings.json")))
                {
                    settingDir = Path.GetDirectoryName(GetAppSettingPath(Path.Combine(settingDir, "appsettings.json")));
                    logger.Debug("アプリケーション設定値を読み出します。パス:{0}", settingDir);
                }
                else
                {
                    settingDir = Path.GetDirectoryName(GetAppSettingPath());
                    logger.Debug("初期値を読み出します。パス:{0}", settingDir);
                }
                logger.Debug("起動パス：{0}", settingDir);

                // アプリケーション設定ファイル読込
                this.AppConfig = new ConfigurationBuilder()
                    .SetBasePath(settingDir)
                    .AddJsonFile(path: "appsettings.json")
                    .Build();

                // NLog設定読込
                LoadNLogConfig(settingDir);
                logger.Debug("アプリケーション設定ファイルを読み込みました。");

                // バインドアドレス読み込み
                this.WebApiServerUrl = GetAppsettingsToSectionStringValue("BindAddress");
                if (string.IsNullOrWhiteSpace(this.WebApiServerUrl))
                {
                    logger.Debug("設定値が存在しないか、空欄です。初期値を使用します。");
                    this.WebApiServerUrl = "http://localhost:5000";
                }
                logger.Debug("バインドアドレス:{0}", this.WebApiServerUrl);

                // ユーザー設定ファイル読込
                if (CheckUserSettingFileExists())
                {
                    settingDir = GetUserSettingPath();
                    logger.Debug("ユーザー設定値を読み出します。パス:{0}", settingDir);
                }
                else
                {
                    settingDir = Directory.GetCurrentDirectory();
                    logger.Debug("初期値を読み出します。パス:{0}", settingDir);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Settingの初期化でエラーが発生しました。メッセージ：{0}", ex.Message);
                throw ex;
            }

            logger.Trace("==============================   End    ==============================");
        }

        /// <summary>
        /// DLLファイル配置先ディレクトリ取得
        /// </summary>
        /// <returns></returns>
        public string GetAssemblyDir()
        {
            string returnVal = "";
            logger.Trace("==============================  Start   ==============================");

            try
            {
                Assembly myAssembly = Assembly.GetEntryAssembly();
                if (myAssembly is not null)
                {
                    if (!string.IsNullOrWhiteSpace(myAssembly.Location))
                    {
                        returnVal = Path.GetDirectoryName(myAssembly.Location);
                    }
                    else
                    {
                        myAssembly = Assembly.GetExecutingAssembly();
                        if (!string.IsNullOrWhiteSpace(myAssembly.Location))
                        {
                            returnVal = Path.GetDirectoryName(myAssembly.Location);
                        }
                        else
                        {
                            myAssembly = Assembly.GetCallingAssembly();
                            if (!string.IsNullOrWhiteSpace(myAssembly.Location))
                            {
                                returnVal = Path.GetDirectoryName(myAssembly.Location);
                            }
                            else
                            {
                                returnVal = AppDomain.CurrentDomain.BaseDirectory;
                            }
                        }
                    }
                }
                else
                {
                    returnVal = null;
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "LoadNLogConfigの初期化でエラーが発生しました。メッセージ：{0}", ex.Message);
                returnVal = null;
            }

            logger.Trace("ファイルパス:{0}", returnVal);
            logger.Trace("==============================   End    ==============================");
            return returnVal;
        }

        /// <summary>
        /// NLog.config読み込み
        /// </summary>
        private void LoadNLogConfig(string settingDir = "")
        {
            logger.Trace("==============================  Start   ==============================");

            try
            {
                string path = "";

                if (string.IsNullOrWhiteSpace(settingDir))
                {
                    path = Path.Combine(GetAssemblyDir(), "NLog.config");
                }
                else
                {
                    path = Path.Combine(settingDir, "NLog.config");
                }

                // NLog設定ファイル存在確認
                if (File.Exists(path))
                {
                    // NLog設定読込
                    this.NLogConfiguration = new XmlLoggingConfiguration(path);
                    NLog.LogManager.Configuration = this.NLogConfiguration;
                    logger.Debug("NLog設定を読み込みました。パス:{0}", path);
                }
                else
                {
                    // appsettings.jsonから読み込み
                    this.NLogConfiguration = new NLogLoggingConfiguration(this.AppConfig.GetSection("NLog"));
                    NLog.LogManager.Configuration = this.NLogConfiguration;
                    logger.Debug("NLog設定を読み込みました。パス:{0}", this.AppConfigPath);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "LoadNLogConfigの初期化でエラーが発生しました。メッセージ：{0}", ex.Message);
            }

            logger.Trace("==============================   End    ==============================");
        }

        /// <summary>
        /// NLog設定取得用メソッド
        /// </summary>
        /// <returns></returns>
        public LoggingConfiguration GetNLogSetting()
        {
            return NLogConfiguration;
        }

        /// <summary>
        /// アプリケーション設定ファイルの存在確認
        /// </summary>
        /// <returns></returns>
        public bool CheckAppSettingFileExists(string settingFilePath = "")
        {
            bool returnVal = false;
            logger.Trace("==============================  Start   ==============================");

            try
            {
                string filePath = GetAppSettingPath();
                if (!string.IsNullOrWhiteSpace(settingFilePath)) { filePath = settingFilePath; }
                logger.Info("アプリケーション設定ファイル確認先：{0}", filePath);
                return File.Exists(filePath);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "CheckAppSettingFileExistsでエラーが発生しました。メッセージ：{0}", ex.Message);
                returnVal = false;
            }

            logger.Trace("==============================   End    ==============================");
            return returnVal;
        }

        /// <summary>
        /// アプリケーション設定ファイルのパス取得
        /// </summary>
        /// <returns></returns>
        private string GetAppSettingPath(string settingFilePath = "")
        {
            string path = "";
            logger.Trace("==============================  Start   ==============================");

            if (!string.IsNullOrWhiteSpace(settingFilePath))
            {
                path = settingFilePath;
            }
            else
            {
                path = Path.Combine(GetAssemblyDir(), "appsettings.json");
            }

            this.AppConfigPath = path;
            logger.Debug("アプリケーション設定ファイル:{0}", this.AppConfigPath);

            logger.Trace("==============================   End    ==============================");
            return this.AppConfigPath;
        }

        /// <summary>
        /// ユーザー設定ファイルの存在確認
        /// </summary>
        /// <returns></returns>
        public bool CheckUserSettingFileExists()
        {
            bool returnVal = false;
            logger.Trace("==============================  Start   ==============================");

            try
            {
                string filePath = GetUserSettingPath();
                logger.Info("ユーザー設定ファイル確認先：{0}", filePath);
                return File.Exists(filePath);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "CheckUserSettingFileExistsでエラーが発生しました。メッセージ：{0}", ex.Message);
                returnVal = false;
            }

            logger.Trace("==============================   End    ==============================");
            return returnVal;
        }

        /// <summary>
        /// ユーザー設定ファイルのパス取得
        /// </summary>
        /// <returns></returns>
        private string GetUserSettingPath()
        {
            logger.Trace("==============================   Call    ==============================");
            this.UserConfigPath = Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), appName), "appsettings.json");
            return this.UserConfigPath;
        }

        /// <summary>
        /// appsettings.jsonの"appsettings"セクションから
        /// 指定したセクションの設定値を文字列で取得
        /// </summary>
        /// <param name="sectionName"></param>
        /// <returns></returns>
        public string GetAppsettingsToSectionStringValue(string sectionName)
        {
            string sectionValue = string.Empty;

            logger.Info("========== Func Start! ==================================================");
            logger.Debug("Parameter[sectionName]：{0}", sectionName);

            try
            {
                IConfigurationSection section = this.AppConfig.GetSection("appSettings");
                sectionValue = section[sectionName];
                logger.Debug("Value                 ：{0}", sectionValue);
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                sectionValue = string.Empty;
            }

            logger.Info("========== Func End!   ==================================================");

            return sectionValue;
        }

        /// <summary>
        /// appsettings.jsonの"appsettings"セクションから
        /// 指定したセクションの設定値を数値で取得
        /// </summary>
        /// <param name="sectionName"></param>
        /// <returns></returns>
        public int? GetAppsettingsToSectionIntValue(string sectionName)
        {
            int? sectionValue = null;

            logger.Info("========== Func Start! ==================================================");
            logger.Debug("Parameter[sectionName]：{0}", sectionName);

            try
            {
                int tmpValue = 0;
                IConfigurationSection section = this.AppConfig.GetSection("appSettings");
                string sectionStringValue = section[sectionName];

                logger.Debug("Value                 ：{0}", sectionStringValue);

                if (int.TryParse(sectionStringValue, out tmpValue))
                {
                    sectionValue = tmpValue;
                }
                else
                {
                    logger.Error("数値への変換に失敗しました。値：{0}", sectionStringValue);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                sectionValue = null;
            }

            logger.Info("========== Func End!   ==================================================");

            return sectionValue;
        }

        /// <summary>
        /// appsettings.jsonの"appsettings"セクションから
        /// 指定したセクションの設定値を文字列で取得
        /// </summary>
        /// <param name="configure"></param>
        /// <param name="sectionName"></param>
        /// <returns></returns>
        public string GetAppsettingsToSectionStringValue(IConfiguration configure, string sectionName)
        {
            string sectionValue = string.Empty;

            logger.Info("========== Func Start! ==================================================");
            logger.Debug("Parameter[sectionName]：{0}", sectionName);

            try
            {
                IConfigurationSection section = configure.GetSection("appSettings");
                sectionValue = section[sectionName];
                logger.Debug("Value                 ：{0}", sectionValue);
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                sectionValue = string.Empty;
            }

            logger.Info("========== Func End!   ==================================================");

            return sectionValue;
        }

        /// <summary>
        /// appsettings.jsonの"appsettings"セクションから
        /// 指定したセクションの設定値を数値で取得
        /// </summary>
        /// <param name="configure"></param>
        /// <param name="sectionName"></param>
        /// <returns></returns>
        public int? GetAppsettingsToSectionIntValue(IConfiguration configure, string sectionName)
        {
            int? sectionValue = null;

            logger.Info("========== Func Start! ==================================================");
            logger.Debug("Parameter[sectionName]：{0}", sectionName);

            try
            {
                int tmpIntValue = 0;
                IConfigurationSection section = configure.GetSection("appSettings");
                string tmpSectionValue = section[sectionName];
                logger.Debug("Value                 ：{0}", tmpSectionValue);

                if (int.TryParse(tmpSectionValue, out tmpIntValue))
                {
                    sectionValue = tmpIntValue;
                }
                else
                {
                    logger.Error("数値への変換に失敗しました。値：{0}", tmpSectionValue);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                sectionValue = null;
            }

            logger.Info("========== Func End!   ==================================================");

            return sectionValue;
        }

    }
}