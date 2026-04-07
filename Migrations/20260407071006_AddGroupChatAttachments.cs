using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FreelancePlatform.Migrations
{
    /// <inheritdoc />
    public partial class AddGroupChatAttachments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AttachmentName",
                table: "GroupChatMessages",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AttachmentType",
                table: "GroupChatMessages",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AttachmentUrl",
                table: "GroupChatMessages",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ParentMessageId",
                table: "GroupChatMessages",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_GroupChatMessages_ParentMessageId",
                table: "GroupChatMessages",
                column: "ParentMessageId");

            migrationBuilder.AddForeignKey(
                name: "FK_GroupChatMessages_GroupChatMessages_ParentMessageId",
                table: "GroupChatMessages",
                column: "ParentMessageId",
                principalTable: "GroupChatMessages",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GroupChatMessages_GroupChatMessages_ParentMessageId",
                table: "GroupChatMessages");

            migrationBuilder.DropIndex(
                name: "IX_GroupChatMessages_ParentMessageId",
                table: "GroupChatMessages");

            migrationBuilder.DropColumn(
                name: "AttachmentName",
                table: "GroupChatMessages");

            migrationBuilder.DropColumn(
                name: "AttachmentType",
                table: "GroupChatMessages");

            migrationBuilder.DropColumn(
                name: "AttachmentUrl",
                table: "GroupChatMessages");

            migrationBuilder.DropColumn(
                name: "ParentMessageId",
                table: "GroupChatMessages");
        }
    }
}
