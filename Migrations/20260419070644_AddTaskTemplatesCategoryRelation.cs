using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FreelancePlatform.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskTemplatesCategoryRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Categories_TaskTemplates_TaskTemplateId",
                table: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_Categories_TaskTemplateId",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "TaskTemplateId",
                table: "Categories");

            migrationBuilder.CreateTable(
                name: "CategoryTaskTemplate",
                columns: table => new
                {
                    CategoriesId = table.Column<int>(type: "integer", nullable: false),
                    TaskTemplatesId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoryTaskTemplate", x => new { x.CategoriesId, x.TaskTemplatesId });
                    table.ForeignKey(
                        name: "FK_CategoryTaskTemplate_Categories_CategoriesId",
                        column: x => x.CategoriesId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CategoryTaskTemplate_TaskTemplates_TaskTemplatesId",
                        column: x => x.TaskTemplatesId,
                        principalTable: "TaskTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CategoryTaskTemplate_TaskTemplatesId",
                table: "CategoryTaskTemplate",
                column: "TaskTemplatesId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CategoryTaskTemplate");

            migrationBuilder.AddColumn<int>(
                name: "TaskTemplateId",
                table: "Categories",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Categories_TaskTemplateId",
                table: "Categories",
                column: "TaskTemplateId");

            migrationBuilder.AddForeignKey(
                name: "FK_Categories_TaskTemplates_TaskTemplateId",
                table: "Categories",
                column: "TaskTemplateId",
                principalTable: "TaskTemplates",
                principalColumn: "Id");
        }
    }
}
