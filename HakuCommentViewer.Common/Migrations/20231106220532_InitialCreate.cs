using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HakuCommentViewer.Common.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CommentInfos",
                columns: table => new
                {
                    StreamInfoId = table.Column<string>(type: "TEXT", nullable: false, comment: "配信情報ID"),
                    TimeStampUSec = table.Column<string>(type: "TEXT", nullable: false, comment: "タイムススタンプ"),
                    CommentId = table.Column<string>(type: "TEXT", nullable: false, comment: "コメント識別用"),
                    StreamSiteId = table.Column<string>(type: "TEXT", nullable: false, comment: "配信サイトID"),
                    StreamManagementId = table.Column<string>(type: "TEXT", nullable: false, comment: "配信管理番号()"),
                    UserId = table.Column<string>(type: "TEXT", nullable: false, comment: "ユーザーID"),
                    UserName = table.Column<string>(type: "TEXT", nullable: false, comment: "ユーザー名"),
                    CommentText = table.Column<string>(type: "TEXT", nullable: false, comment: "コメント")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommentInfos", x => new { x.StreamInfoId, x.TimeStampUSec, x.CommentId });
                },
                comment: "コメント情報テーブル");

            migrationBuilder.CreateTable(
                name: "GiftInfos",
                columns: table => new
                {
                    TimeStampUSec = table.Column<string>(type: "TEXT", nullable: false, comment: "タイムススタンプ"),
                    CommentId = table.Column<string>(type: "TEXT", nullable: false, comment: "コメント識別用"),
                    StreamSiteId = table.Column<string>(type: "TEXT", nullable: false, comment: "配信サイトID"),
                    StreamInfoId = table.Column<string>(type: "TEXT", nullable: false),
                    StreamManagementId = table.Column<string>(type: "TEXT", nullable: false, comment: "配信管理ID"),
                    UserId = table.Column<string>(type: "TEXT", nullable: false, comment: "ユーザーID"),
                    UserName = table.Column<string>(type: "TEXT", nullable: false, comment: "ユーザー名"),
                    GiftType = table.Column<string>(type: "TEXT", nullable: false, comment: "スパ茶・ギフト種別"),
                    GiftValue = table.Column<int>(type: "INTEGER", nullable: false, comment: "スパ茶・ギフト種別"),
                    CommentText = table.Column<string>(type: "TEXT", nullable: false, comment: "コメント")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GiftInfos", x => new { x.StreamSiteId, x.TimeStampUSec, x.CommentId });
                },
                comment: "スパ茶・ギフト情報テーブル");

            migrationBuilder.CreateTable(
                name: "StreamInfos",
                columns: table => new
                {
                    StreamId = table.Column<string>(type: "TEXT", nullable: false, comment: "配信情報ID"),
                    StreamName = table.Column<string>(type: "TEXT", nullable: true, comment: "配信名"),
                    StreamUrl = table.Column<string>(type: "TEXT", nullable: true, comment: "配信URL"),
                    CommentRoomId = table.Column<string>(type: "TEXT", nullable: true, comment: "チャット(コメント)ルームID"),
                    StreamSiteId = table.Column<string>(type: "TEXT", nullable: true, comment: "配信サイトID"),
                    StartDateTime = table.Column<DateTime>(type: "TEXT", nullable: true, comment: "配信開始日時"),
                    EndDateTime = table.Column<DateTime>(type: "TEXT", nullable: true, comment: "配信終了日時"),
                    ViewerCount = table.Column<int>(type: "INTEGER", nullable: true, comment: "視聴者数"),
                    CommentCount = table.Column<int>(type: "INTEGER", nullable: true, comment: "コメント数"),
                    Note = table.Column<string>(type: "TEXT", nullable: true, comment: "備考")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StreamInfos", x => x.StreamId);
                },
                comment: "配信情報管理テーブル");

            migrationBuilder.CreateTable(
                name: "StreamManagementInfos",
                columns: table => new
                {
                    StreamManagementId = table.Column<string>(type: "TEXT", nullable: false),
                    StreamId = table.Column<string>(type: "TEXT", nullable: true, comment: "配信情報ID"),
                    StreamNo = table.Column<int>(type: "TEXT", nullable: false, comment: "配信管理番号(画面用)"),
                    IsConnected = table.Column<bool>(type: "INTEGER", nullable: false, comment: "接続済みフラグ"),
                    UseCommentGenerator = table.Column<bool>(type: "INTEGER", nullable: false, comment: "コメントジェネレーター機能利用フラグ"),
                    UseNarrator = table.Column<bool>(type: "INTEGER", nullable: false, comment: "読上げ機能利用フラグ"),
                    Note = table.Column<string>(type: "TEXT", nullable: true, comment: "備考")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StreamManagementInfos", x => x.StreamManagementId);
                },
                comment: "配信管理情報管理テーブル");

            migrationBuilder.CreateTable(
                name: "UserInfos",
                columns: table => new
                {
                    StreamSiteId = table.Column<string>(type: "TEXT", nullable: false, comment: "配信サイトID"),
                    UserId = table.Column<string>(type: "TEXT", nullable: false, comment: "ユーザーID"),
                    UserName = table.Column<string>(type: "TEXT", nullable: false, comment: "ユーザー名"),
                    IconPath = table.Column<string>(type: "TEXT", nullable: true, comment: "アイコン配置先"),
                    Note = table.Column<string>(type: "TEXT", nullable: true, comment: "備考")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserInfos", x => new { x.StreamSiteId, x.UserId });
                },
                comment: "ユーザー情報管理テーブル");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CommentInfos");

            migrationBuilder.DropTable(
                name: "GiftInfos");

            migrationBuilder.DropTable(
                name: "StreamInfos");

            migrationBuilder.DropTable(
                name: "StreamManagementInfos");

            migrationBuilder.DropTable(
                name: "UserInfos");
        }
    }
}
