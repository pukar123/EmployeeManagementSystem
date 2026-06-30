using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EMS.Domain.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeEmploymentStatusHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmployeeEmploymentStatusHistories",
                schema: "org",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    PreviousStatus = table.Column<int>(type: "int", nullable: true),
                    NewStatus = table.Column<int>(type: "int", nullable: false),
                    EffectiveDateUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    ChangedByUserId = table.Column<int>(type: "int", nullable: true),
                    ChangedByUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ChangedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeEmploymentStatusHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeEmploymentStatusHistories_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "org",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeEmploymentStatusHistories_CreatedAtUtc",
                schema: "org",
                table: "EmployeeEmploymentStatusHistories",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeEmploymentStatusHistories_EffectiveDateUtc",
                schema: "org",
                table: "EmployeeEmploymentStatusHistories",
                column: "EffectiveDateUtc");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeEmploymentStatusHistories_EmployeeId",
                schema: "org",
                table: "EmployeeEmploymentStatusHistories",
                column: "EmployeeId");

            migrationBuilder.Sql("""
                INSERT INTO org.EmployeeEmploymentStatusHistories
                    (EmployeeId, PreviousStatus, NewStatus, EffectiveDateUtc, Reason, CreatedAtUtc)
                SELECT
                    e.Id,
                    NULL,
                    e.EmploymentStatus,
                    e.DateJoined,
                    N'Backfilled from existing employment status.',
                    GETUTCDATE()
                FROM org.Employees e
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM org.EmployeeEmploymentStatusHistories h
                    WHERE h.EmployeeId = e.Id
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmployeeEmploymentStatusHistories",
                schema: "org");
        }
    }
}
