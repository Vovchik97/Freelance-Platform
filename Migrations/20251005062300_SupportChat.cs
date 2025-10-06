using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FreelancePlatform.Migrations
{
    /// <inheritdoc />
    public partial class SupportChat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SenderName",
                table: "Messages",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdminId",
                table: "Chats",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsBotActive",
                table: "Chats",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsSupport",
                table: "Chats",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SenderName",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "AdminId",
                table: "Chats");

            migrationBuilder.DropColumn(
                name: "IsBotActive",
                table: "Chats");

            migrationBuilder.DropColumn(
                name: "IsSupport",
                table: "Chats");
        }
    }
}
