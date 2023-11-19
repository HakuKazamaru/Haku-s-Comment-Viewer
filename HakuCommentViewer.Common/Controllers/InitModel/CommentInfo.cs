using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

namespace HakuCommentViewer.Common.Controllers.InitModel
{
    /// <summary>
    /// コメント情報管理データモデル初期化
    /// </summary>
    public class CommentInfo
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
            modelBuilder.Entity<Models.CommentInfo>(entity =>
            {
                // 主キー設定
                entity.HasKey(e => new { e.StreamInfoId, e.TimeStampUSec, e.CommentId });

                // テーブル名の設定
                entity.HasComment("コメント情報テーブル");

                // 配信情報ID
                entity.Property(c => c.StreamInfoId)
                    .HasColumnType("TEXT")
                    .HasComment("配信情報ID");
                // タイムススタンプ(コメント識別用)
                entity.Property(c => c.TimeStampUSec)
                    .HasColumnType("TEXT")
                    .HasComment("タイムススタンプ");
                // コメント識別用
                entity.Property(c => c.CommentId)
                    .HasColumnType("TEXT")
                    .HasComment("コメント識別用");
                // 配信サイトID
                entity.Property(c => c.StreamSiteId)
                    .HasColumnType("TEXT")
                    .HasComment("配信サイトID");
                // 配信管理番号
                entity.Property(c => c.StreamManagementId)
                    .HasColumnType("TEXT")
                    .HasComment("配信管理番号()");
                // ユーザーID
                entity.Property(c => c.UserId)
                    .HasColumnType("TEXT")
                    .HasComment("ユーザーID");
                // ユーザー名
                entity.Property(c => c.UserName)
                    .HasColumnType("TEXT")
                    .HasComment("ユーザー名");
                // コメント
                entity.Property(c => c.CommentText)
                    .HasColumnType("TEXT")
                    .HasComment("コメント");
            });
            logger.Trace("========== Func End!   ==================================================");
        }
    }
}
