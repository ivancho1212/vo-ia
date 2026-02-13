using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voia.Api.Migrations
{
    public partial class AddWidgetWidthHeight : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "width",
                table: "bot_styles",
                type: "int",
                nullable: true,
                defaultValue: 380);

            migrationBuilder.AddColumn<int>(
                name: "height",
                table: "bot_styles",
                type: "int",
                nullable: true,
                defaultValue: 600);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "width",
                table: "bot_styles");

            migrationBuilder.DropColumn(
                name: "height",
                table: "bot_styles");
        }
    }
}
