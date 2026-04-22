using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FreelancePlatform.Migrations
{
    /// <inheritdoc />
    public partial class RenameTaskTemplateItemToTaskTemplateItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TaskTemplateItem_TaskTemplates_TaskTemplateId",
                table: "TaskTemplateItem");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TaskTemplateItem",
                table: "TaskTemplateItem");

            migrationBuilder.RenameTable(
                name: "TaskTemplateItem",
                newName: "TaskTemplateItems");

            migrationBuilder.RenameIndex(
                name: "IX_TaskTemplateItem_TaskTemplateId",
                table: "TaskTemplateItems",
                newName: "IX_TaskTemplateItems_TaskTemplateId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TaskTemplateItems",
                table: "TaskTemplateItems",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TaskTemplateItems_TaskTemplates_TaskTemplateId",
                table: "TaskTemplateItems",
                column: "TaskTemplateId",
                principalTable: "TaskTemplates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TaskTemplateItems_TaskTemplates_TaskTemplateId",
                table: "TaskTemplateItems");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TaskTemplateItems",
                table: "TaskTemplateItems");

            migrationBuilder.RenameTable(
                name: "TaskTemplateItems",
                newName: "TaskTemplateItem");

            migrationBuilder.RenameIndex(
                name: "IX_TaskTemplateItems_TaskTemplateId",
                table: "TaskTemplateItem",
                newName: "IX_TaskTemplateItem_TaskTemplateId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TaskTemplateItem",
                table: "TaskTemplateItem",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TaskTemplateItem_TaskTemplates_TaskTemplateId",
                table: "TaskTemplateItem",
                column: "TaskTemplateId",
                principalTable: "TaskTemplates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
