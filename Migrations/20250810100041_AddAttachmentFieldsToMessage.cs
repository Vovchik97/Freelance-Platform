using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FreelancePlatform.Migrations
{
    /// <inheritdoc />
    public partial class AddAttachmentFieldsToMessage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Services_AspNetUsers_ClientId",
                table: "Services");

            migrationBuilder.DropIndex(
                name: "IX_Services_ClientId",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "ClientId",
                table: "Services");

            migrationBuilder.AddColumn<string>(
                name: "AttachmentName",
                table: "Messages",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AttachmentType",
                table: "Messages",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AttachmentUrl",
                table: "Messages",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AttachmentName",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "AttachmentType",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "AttachmentUrl",
                table: "Messages");

            migrationBuilder.AddColumn<string>(
                name: "ClientId",
                table: "Services",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Services_ClientId",
                table: "Services",
                column: "ClientId");

            migrationBuilder.AddForeignKey(
                name: "FK_Services_AspNetUsers_ClientId",
                table: "Services",
                column: "ClientId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }
    }
}
