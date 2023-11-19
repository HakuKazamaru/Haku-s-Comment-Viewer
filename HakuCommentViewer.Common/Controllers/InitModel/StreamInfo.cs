using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

namespace HakuCommentViewer.Common.Controllers.InitModel
{
    /// <summary>
    /// 配信情報管理データモデル初期化
    /// </summary>
    internal class StreamInfo
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

            // 配信情報管理テーブル
            modelBuilder.Entity<Models.StreamInfo>(entity =>
            {
                // 主キー設定
                entity.HasKey(e => new { e.StreamId });

                // テーブル名の設定
                entity.HasComment("配信情報管理テーブル");

                // 配信管理番号
                entity.Property(c => c.StreamId)
                    .HasColumnType("TEXT")
                    .HasComment("配信情報ID");
                // 配信ID(配信サイト内ID)
                entity.Property(c => c.StreamPageId)
                    .HasColumnType("TEXT")
                    .HasComment("配信ID(配信サイト内ID)");
                // 配信名
                entity.Property(c => c.StreamName)
                    .HasColumnType("TEXT")
                    .HasComment("配信名");
                // 配信URL
                entity.Property(c => c.StreamUrl)
                    .HasColumnType("TEXT")
                    .HasComment("配信URL");
                // チャット(コメント)ルームID
                entity.Property(c => c.CommentRoomId)
                    .HasColumnType("TEXT")
                    .HasComment("チャット(コメント)ルームID");
                // 配信サイトID
                entity.Property(c => c.StreamSiteId)
                    .HasColumnType("TEXT")
                    .HasComment("配信サイトID");
                // 配信開始日時
                entity.Property(c => c.StartDateTime)
                    .HasColumnType("TEXT")
                    .HasComment("配信開始日時");
                // 配信終了日時
                entity.Property(c => c.EndDateTime)
                    .HasColumnType("TEXT")
                    .HasComment("配信終了日時");
                // 視聴者数
                entity.Property(c => c.ViewerCount)
                    .HasColumnType("INTEGER")
                    .HasComment("視聴者数");
                // コメント数
                entity.Property(c => c.CommentCount)
                    .HasColumnType("INTEGER")
                    .HasComment("コメント数");
                // 配信ステータス
                entity.Property(c => c.LiveStatus)
                    .HasColumnType("INTEGER")
                    .HasComment("配信ステータス");
                // 備考
                entity.Property(c => c.Note)
                    .HasColumnType("TEXT")
                    .HasComment("備考");
            });
            logger.Trace("========== Func End!   ==================================================");
        }
    }
}
