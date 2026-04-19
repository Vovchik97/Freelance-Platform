using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FreelancePlatform.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TaskTemplateId",
                table: "Projects",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TaskTemplateId",
                table: "Orders",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TaskTemplateId",
                table: "Categories",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TaskTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProjectId = table.Column<int>(type: "integer", nullable: true),
                    OrderId = table.Column<int>(type: "integer", nullable: true),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    OrderIndex = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkItems_AspNetUsers_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorkItems_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WorkItems_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TaskTemplateItem",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TaskTemplateId = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    OrderIndex = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskTemplateItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskTemplateItem_TaskTemplates_TaskTemplateId",
                        column: x => x.TaskTemplateId,
                        principalTable: "TaskTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Projects_TaskTemplateId",
                table: "Projects",
                column: "TaskTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_TaskTemplateId",
                table: "Orders",
                column: "TaskTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_TaskTemplateId",
                table: "Categories",
                column: "TaskTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskTemplateItem_TaskTemplateId",
                table: "TaskTemplateItem",
                column: "TaskTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkItems_CreatedById",
                table: "WorkItems",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_WorkItems_OrderId",
                table: "WorkItems",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkItems_ProjectId",
                table: "WorkItems",
                column: "ProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_Categories_TaskTemplates_TaskTemplateId",
                table: "Categories",
                column: "TaskTemplateId",
                principalTable: "TaskTemplates",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_TaskTemplates_TaskTemplateId",
                table: "Orders",
                column: "TaskTemplateId",
                principalTable: "TaskTemplates",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_TaskTemplates_TaskTemplateId",
                table: "Projects",
                column: "TaskTemplateId",
                principalTable: "TaskTemplates",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Categories_TaskTemplates_TaskTemplateId",
                table: "Categories");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_TaskTemplates_TaskTemplateId",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_Projects_TaskTemplates_TaskTemplateId",
                table: "Projects");

            migrationBuilder.DropTable(
                name: "TaskTemplateItem");

            migrationBuilder.DropTable(
                name: "WorkItems");

            migrationBuilder.DropTable(
                name: "TaskTemplates");

            migrationBuilder.DropIndex(
                name: "IX_Projects_TaskTemplateId",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_Orders_TaskTemplateId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Categories_TaskTemplateId",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "TaskTemplateId",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "TaskTemplateId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "TaskTemplateId",
                table: "Categories");
        }
    }
}
