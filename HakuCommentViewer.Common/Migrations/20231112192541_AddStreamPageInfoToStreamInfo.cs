using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HakuCommentViewer.Common.Migrations
{
    public partial class AddStreamPageInfoToStreamInfo : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StreamPageId",
                table: "StreamInfos",
                type: "TEXT",
                nullable: true,
                comment: "配信ID(配信サイト内ID)");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StreamPageId",
                table: "StreamInfos");
        }
    }
}
