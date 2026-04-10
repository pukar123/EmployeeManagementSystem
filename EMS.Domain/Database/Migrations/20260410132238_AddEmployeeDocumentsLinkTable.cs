using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EMS.Domain.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeDocumentsLinkTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmployeeDocuments",
                schema: "org",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    DocumentId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeDocuments_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalSchema: "org",
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeDocuments_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "org",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeDocuments_DocumentId",
                schema: "org",
                table: "EmployeeDocuments",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeDocuments_EmployeeId",
                schema: "org",
                table: "EmployeeDocuments",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeDocuments_EmployeeId_DocumentId",
                schema: "org",
                table: "EmployeeDocuments",
                columns: new[] { "EmployeeId", "DocumentId" },
                unique: true);

            migrationBuilder.Sql(
                """
                INSERT INTO [org].[EmployeeDocuments] ([EmployeeId], [DocumentId], [IsActive], [IsDeleted], [CreatedAtUtc], [UpdatedAtUtc])
                SELECT [EmployeeId], [Id], 1, 0, GETUTCDATE(), GETUTCDATE()
                FROM [org].[Documents]
                WHERE [EmployeeId] IS NOT NULL;
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_Documents_Employees_EmployeeId",
                schema: "org",
                table: "Documents");

            migrationBuilder.DropIndex(
                name: "IX_Documents_EmployeeId",
                schema: "org",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "EmployeeId",
                schema: "org",
                table: "Documents");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmployeeDocuments",
                schema: "org");

            migrationBuilder.AddColumn<int>(
                name: "EmployeeId",
                schema: "org",
                table: "Documents",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Documents_EmployeeId",
                schema: "org",
                table: "Documents",
                column: "EmployeeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Documents_Employees_EmployeeId",
                schema: "org",
                table: "Documents",
                column: "EmployeeId",
                principalSchema: "org",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
