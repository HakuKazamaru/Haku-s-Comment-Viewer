using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HakuCommentViewer.Common.Controllers.InitModel
{
    /// <summary>
    /// 配信管理情報データモデル初期化
    /// </summary>
    internal class StreamManagementInfo
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

            // 配信管理情報管理テーブル
            modelBuilder.Entity<Models.StreamManagementInfo>(entity =>
            {
                // 主キー設定
                entity.HasKey(e => new { e.StreamManagementId });

                // テーブル名の設定
                entity.HasComment("配信管理情報管理テーブル");

                // 配信管理番号
                entity.Property(c => c.StreamId)
                    .HasColumnType("TEXT")
                    .HasComment("配信情報ID");
                // 配信管理番号
                entity.Property(c => c.StreamId)
                    .HasColumnType("TEXT")
                    .HasComment("配信情報ID");
                // 配信サイトID
                entity.Property(c => c.StreamNo)
                    .HasColumnType("INTEGER")
                    .HasComment("配信サイトID");
                // 配信管理番号(画面用)
                entity.Property(c => c.StreamNo)
                    .HasColumnType("TEXT")
                    .HasComment("配信管理番号(画面用)");
                // 接続済みフラグ
                entity.Property(c => c.IsConnected)
                    .HasColumnType("INTEGER")
                    .HasComment("接続済みフラグ");
                // 開始/停止要求
                entity.Property(c => c.ControlRequest)
                    .HasColumnType("INTEGER")
                    .HasComment("開始/停止要求");
                // コメントジェネレーター機能利用フラグ
                entity.Property(c => c.UseCommentGenerator)
                    .HasColumnType("INTEGER")
                    .HasComment("コメントジェネレーター機能利用フラグ");
                // 読上げ機能利用フラグ
                entity.Property(c => c.UseNarrator)
                    .HasColumnType("INTEGER")
                    .HasComment("読上げ機能利用フラグ");
                // 備考
                entity.Property(c => c.Note)
                    .HasColumnType("TEXT")
                    .HasComment("備考");
            });
            logger.Trace("========== Func End!   ==================================================");
        }
    }
}
