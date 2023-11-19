using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HakuCommentViewer.Common.Controllers.InitModel
{
    /// <summary>
    /// スパ茶・ギフト情報管理データモデル初期化
    /// </summary>
    public class GiftInfo
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
            modelBuilder.Entity<Models.GiftInfo>(entity =>
            {
                // 主キー設定
                entity.HasKey(e => new { e.StreamSiteId, e.TimeStampUSec, e.CommentId });

                // テーブル名の設定
                entity.HasComment("スパ茶・ギフト情報テーブル");

                // 配信サイトID
                entity.Property(c => c.StreamSiteId)
                    .HasColumnType("TEXT")
                    .HasComment("配信サイトID");
                // タイムススタンプ(コメント識別用)
                entity.Property(c => c.TimeStampUSec)
                    .HasColumnType("TEXT")
                    .HasComment("タイムススタンプ");
                // コメント識別用
                entity.Property(c => c.CommentId)
                    .HasColumnType("TEXT")
                    .HasComment("コメント識別用");
                // 配信管理ID
                entity.Property(c => c.StreamManagementId)
                    .HasColumnType("TEXT")
                    .HasComment("配信管理ID");
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
                // スパ茶・ギフト金額
                entity.Property(c => c.GiftType)
                    .HasColumnType("TEXT")
                    .HasComment("スパ茶・ギフト種別");
                // スパ茶・ギフト金額
                entity.Property(c => c.GiftValue)
                    .HasColumnType("INTEGER")
                    .HasComment("スパ茶・ギフト種別");
            });
            logger.Trace("========== Func End!   ==================================================");
        }
    }
}
