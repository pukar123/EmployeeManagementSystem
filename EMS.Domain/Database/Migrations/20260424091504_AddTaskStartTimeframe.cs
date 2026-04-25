using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EMS.Domain.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskStartTimeframe : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "StartAtUtc",
                schema: "org",
                table: "Tasks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_StartAtUtc",
                schema: "org",
                table: "Tasks",
                column: "StartAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tasks_StartAtUtc",
                schema: "org",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "StartAtUtc",
                schema: "org",
                table: "Tasks");
        }
    }
}
