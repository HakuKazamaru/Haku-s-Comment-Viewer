using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HakuCommentViewer.Common.Migrations
{
    public partial class AddControlRequestToStreamManagementInfo : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ControlRequest",
                table: "StreamManagementInfos",
                type: "INTEGER",
                nullable: true,
                comment: "開始/停止要求");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ControlRequest",
                table: "StreamManagementInfos");
        }
    }
}
