using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MLog = Microsoft.Extensions.Logging;

using NLog;
// using NLog.Web;
using NLog.Extensions.Logging;

using HakuCommentViewer.Common.Models;

namespace HakuCommentViewer.Common
{
    /// <summary>
    /// DBコンテキストクラス
    /// </summary>
    public class HcvDbContext : DbContext
    {
        /// <summary>
        /// NLogロガー
        /// </summary>
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        /// <summary>
        /// SQLite DBファイルパス
        /// </summary>
        public string DbFilePath { get; }

        #region データモデル宣言
        /// <summary>
        /// コメント情報
        /// </summary>
        public DbSet<CommentInfo> CommentInfos { get; set; }

        /// <summary>
        /// スパ茶・ギフト情報
        /// </summary>
        public DbSet<GiftInfo> GiftInfos { get; set; }

        /// <summary>
        /// 配信情報
        /// </summary>
        public DbSet<StreamInfo> StreamInfos { get; set; }

        /// <summary>
        /// 配信管理情報
        /// </summary>
        public DbSet<StreamManagementInfo> StreamManagementInfos { get; set; }

        /// <summary>
        /// ユーザー情報
        /// </summary>
        public DbSet<UserInfo> UserInfos { get; set; }
        #endregion

        /// <summary>
        /// コンストラクター
        /// </summary>
        public HcvDbContext()
        {
            logger.Trace("==============================  Start   ==============================");
            string rootFolderPath, exeDataFolderPath, dbDataFolderPath, exeFileName;

            if (string.IsNullOrWhiteSpace(this.DbFilePath))
            {
                logger.Trace("DBファイルパスが指定されていないため、初期値を使用します。");

                exeFileName = Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().GetName().Name);
                rootFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                exeDataFolderPath = Path.Combine(rootFolderPath, exeFileName);
                dbDataFolderPath = Path.Combine(exeDataFolderPath, "Database");
                this.DbFilePath = Path.Combine(dbDataFolderPath, exeFileName + ".db");
            }

            logger.Trace("DBファイルパス　　　　　　:{0}", this.DbFilePath);

            (bool fileExits, int errorFlag, string[] notExitsDirPathList) = CheckDbFileExist(this.DbFilePath);

            if (!fileExits && errorFlag == 2)
            {
                logger.Info("DB格納フォルダーが存在しません。");

                foreach (var notExitsDirPath in notExitsDirPathList.Select((Value, Index) => (Value, Index)))
                {
                    Directory.CreateDirectory(notExitsDirPath.Value);
                    logger.Info("[{0}]データ格納用フォルダーを作成しました。パス:{1}", notExitsDirPath.Index, notExitsDirPath.Value);
                }
            }
            else if (!fileExits && errorFlag != 1)
            {
                throw new Exception("DBファイルの存在確認で不明なエラーが発生しました。");
            }

            logger.Trace("==============================   End    ==============================");
        }

        /// <summary>
        /// コンストラクター(引数あり)
        /// </summary>
        /// <param name="dbFilePath">DBファイル保存先</param>
        public HcvDbContext(string dbFilePath) : this()
        {
            logger.Trace("==============================  Start   ==============================");

            logger.Trace("指定DBファイルパス:{0}", dbFilePath);
            this.DbFilePath = dbFilePath;

            logger.Trace("==============================   End    ==============================");
        }

        /// <summary>
        /// DbContext設定イベント処理
        /// </summary>
        /// <param name="options"></param>
        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            logger.Trace("========== Func Start! ==================================================");

            base.OnConfiguring(options);

            // ロガーの設定
            var loggerFactory = MLog.LoggerFactory.Create(builder =>
            {
                builder
                    .AddProvider(new NLogLoggerProvider())
                    .AddFilter((category, level) =>
                        category == DbLoggerCategory.Database.Command.Name &&
                        level == MLog.LogLevel.Information);
            });

            options
                .LogTo(message => logger.Trace(message),
                new[] { DbLoggerCategory.Database.Name },
                MLog.LogLevel.Debug,
                Microsoft.EntityFrameworkCore.Diagnostics.DbContextLoggerOptions.LocalTime)
                .UseSqlite($"Data Source={this.DbFilePath}");
            logger.Trace($"Data Source={this.DbFilePath}");

            logger.Trace("========== Func End!   ==================================================");
        }

        /// <summary>
        /// データモデル生成時処理
        /// </summary>
        /// <param name="modelBuilder"></param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            logger.Debug("========== Func Start! ==================================================");

            base.OnModelCreating(modelBuilder);

            Controllers.InitModel.CommentInfo.Init(ref modelBuilder);
            Controllers.InitModel.GiftInfo.Init(ref modelBuilder);
            Controllers.InitModel.StreamInfo.Init(ref modelBuilder);
            Controllers.InitModel.StreamManagementInfo.Init(ref modelBuilder);
            Controllers.InitModel.UserInfo.Init(ref modelBuilder);

            logger.Debug("========== Func End!   ==================================================");
        }

        /// <summary>
        /// DBファイル存在確認
        /// </summary>
        /// <param name="dbFilePath"></param>
        /// <returns></returns>
        public static (bool, int, string[]) CheckDbFileExist(string dbFilePath)
        {
            bool returnVal1 = false;
            int returnVal2 = -1;
            string[] returnVal3 = null;
            logger.Trace("==============================  Start   ==============================");

            logger.Trace("確認対象:{0}", dbFilePath);
            if (!string.IsNullOrWhiteSpace(dbFilePath))
            {
                string rootFolderPath, exeDataFolderPath, dbDataFolderPath, exeFileName;

                exeFileName = Path.GetFileNameWithoutExtension(dbFilePath);
                dbDataFolderPath = Path.GetDirectoryName(dbFilePath);
                exeDataFolderPath = Path.GetDirectoryName(dbDataFolderPath);
                rootFolderPath = Path.GetDirectoryName(exeDataFolderPath);

                logger.Trace("----------------------------------------------------------------------");
                logger.Trace("ベースフォルダーパス　　　　:{0}", rootFolderPath);
                logger.Trace("アプリデータフォルダーパス　:{0}", exeDataFolderPath);
                logger.Trace("DBファイル格納フォルダーパス:{0}", dbDataFolderPath);
                logger.Trace("----------------------------------------------------------------------");
                logger.Trace("DBファイル名　　　　　　　　:{0}.db", exeFileName);
                logger.Trace("----------------------------------------------------------------------");

                if (File.Exists(dbFilePath))
                {
                    returnVal1 = true;
                    returnVal2 = 0;
                }
                else
                {
                    List<string> pathList = new List<string>();
                    returnVal1 = false;

                    if (!Directory.Exists(rootFolderPath))
                    {
                        throw new DirectoryNotFoundException(
                            string.Format("ベースフォルダーが存在していません。パス:{0}", rootFolderPath));
                    }

                    if (!Directory.Exists(exeDataFolderPath))
                    {
                        logger.Trace("プリケーションデータ格納用フォルダーが存在しません。パス:{0}", exeDataFolderPath);
                        pathList.Add(exeDataFolderPath);
                    }

                    if (!Directory.Exists(dbDataFolderPath))
                    {
                        logger.Trace("DBファイル格納用フォルダーが存在しません。パス:{0}", dbDataFolderPath);
                        pathList.Add(dbDataFolderPath);
                    }

                    returnVal2 = pathList.Count > 0 ? 2 : 1;
                    returnVal3 = pathList.ToArray();
                }
            }
            else
            {
                throw new ArgumentNullException("ファイルパスが空白です。");
            }

            logger.Trace("返値:{0},{1},{2}", returnVal1, returnVal2, returnVal3 != null ? returnVal3.Length : "NULL");
            logger.Trace("==============================   End    ==============================");
            return (returnVal1, returnVal2, returnVal3);
        }

    }
}
