using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voia.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailNotificationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "assigned_user_id",
                table: "Conversations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "unread_admin_messages",
                table: "Conversations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_assigned_user_id",
                table: "Conversations",
                column: "assigned_user_id");

            migrationBuilder.AddForeignKey(
                name: "FK_Conversations_AspNetUsers_assigned_user_id",
                table: "Conversations",
                column: "assigned_user_id",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Conversations_AspNetUsers_assigned_user_id",
                table: "Conversations");

            migrationBuilder.DropIndex(
                name: "IX_Conversations_assigned_user_id",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "assigned_user_id",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "unread_admin_messages",
                table: "Conversations");
        }
    }
}
