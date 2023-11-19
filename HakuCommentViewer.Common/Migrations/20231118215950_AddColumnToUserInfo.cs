using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HakuCommentViewer.Common.Migrations
{
    public partial class AddColumnToUserInfo : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CommentCount",
                table: "UserInfos",
                type: "INTEGER",
                nullable: true,
                comment: "累計コメント数");

            migrationBuilder.AddColumn<DateTime>(
                name: "FastCommentDateTime",
                table: "UserInfos",
                type: "TEXT",
                nullable: true,
                comment: "初回コメント日時");

            migrationBuilder.AddColumn<int>(
                name: "GiftCount",
                table: "UserInfos",
                type: "INTEGER",
                nullable: true,
                comment: "累計スパ茶/ギフト数");

            migrationBuilder.AddColumn<string>(
                name: "HandleName",
                table: "UserInfos",
                type: "TEXT",
                nullable: true,
                comment: "ハンドルネーム");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastCommentDateTime",
                table: "UserInfos",
                type: "TEXT",
                nullable: true,
                comment: "最終コメント日時");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CommentCount",
                table: "UserInfos");

            migrationBuilder.DropColumn(
                name: "FastCommentDateTime",
                table: "UserInfos");

            migrationBuilder.DropColumn(
                name: "GiftCount",
                table: "UserInfos");

            migrationBuilder.DropColumn(
                name: "HandleName",
                table: "UserInfos");

            migrationBuilder.DropColumn(
                name: "LastCommentDateTime",
                table: "UserInfos");
        }
    }
}
