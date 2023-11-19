using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HakuCommentViewer.Common.Controllers.InitModel
{
    /// <summary>
    /// ユーザー情報管理データモデル初期化
    /// </summary>
    public class UserInfo
    {
        /// <summary>
        /// NLogロガー
        /// </summary>
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        /// <summary>
        /// データモデル初期化処理
        /// </summary>
        /// <param name="modelBuilder"></param>
        public static void Init(ref ModelBuilder modelBuilder)
        {
            logger.Trace("========== Func Start! ==================================================");
            // チャットログ管理テーブル
            modelBuilder.Entity<Models.UserInfo>(entity =>
            {
                // 主キー設定
                entity.HasKey(e => new {e.StreamSiteId, e.UserId });

                // テーブル名の設定
                entity.HasComment("ユーザー情報管理テーブル");

                // 配信サイトID
                entity.Property(c => c.StreamSiteId)
                    .HasColumnType("TEXT")
                    .HasComment("配信サイトID");
                // ユーザーID
                entity.Property(c => c.UserId)
                    .HasColumnType("TEXT")
                    .HasComment("ユーザーID");
                // ユーザー名
                entity.Property(c => c.UserName)
                    .HasColumnType("TEXT")
                    .HasComment("ユーザー名");
                // ユーザー名
                entity.Property(c => c.HandleName)
                    .HasColumnType("TEXT")
                    .HasComment("ハンドルネーム");
                // アイコン配置先
                entity.Property(c => c.IconPath)
                    .HasColumnType("TEXT")
                    .HasComment("アイコン配置先");
                // 累計コメント数
                entity.Property(c => c.CommentCount)
                    .HasColumnType("INTEGER")
                    .HasComment("累計コメント数");
                // 累計スパ茶/ギフト数
                entity.Property(c => c.GiftCount)
                    .HasColumnType("INTEGER")
                    .HasComment("累計スパ茶/ギフト数");
                // 初回コメント日時
                entity.Property(c => c.FastCommentDateTime)
                    .HasColumnType("TEXT")
                    .HasComment("初回コメント日時");
                // 最終コメント日時
                entity.Property(c => c.LastCommentDateTime)
                    .HasColumnType("TEXT")
                    .HasComment("最終コメント日時");
                // 備考
                entity.Property(c => c.Note)
                    .HasColumnType("TEXT")
                    .HasComment("備考");
            });
            logger.Trace("========== Func End!   ==================================================");
        }

    }
}
