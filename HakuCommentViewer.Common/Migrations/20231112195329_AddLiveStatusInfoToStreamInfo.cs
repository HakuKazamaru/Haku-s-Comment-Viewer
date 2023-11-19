using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HakuCommentViewer.Common.Migrations
{
    public partial class AddLiveStatusInfoToStreamInfo : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LiveStatus",
                table: "StreamInfos",
                type: "INTEGER",
                nullable: true,
                comment: "配信ステータス");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LiveStatus",
                table: "StreamInfos");
        }
    }
}
