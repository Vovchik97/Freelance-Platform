﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FreelancePlatform.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectToPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProjectId",
                table: "Payments",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_ProjectId",
                table: "Payments",
                column: "ProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Projects_ProjectId",
                table: "Payments",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Projects_ProjectId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_ProjectId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "Payments");
        }
    }
}
