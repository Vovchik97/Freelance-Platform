using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FreelancePlatform.Migrations
{
    /// <inheritdoc />
    public partial class AddTakenByToProjectTask : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TakenByUserId",
                table: "ProjectTasks",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TakenByUserName",
                table: "ProjectTasks",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TakenByUserId",
                table: "ProjectTasks");

            migrationBuilder.DropColumn(
                name: "TakenByUserName",
                table: "ProjectTasks");
        }
    }
}
