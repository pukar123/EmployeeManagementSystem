using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EMS.Domain.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddPositionRoleAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmployeeRoleAssignments",
                schema: "org",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    Source = table.Column<int>(type: "int", nullable: false),
                    JobPositionId = table.Column<int>(type: "int", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeRoleAssignments", x => x.Id);
                    table.CheckConstraint("CK_EmployeeRoleAssignments_SourceJobPosition", "([Source] = 1 AND [JobPositionId] IS NOT NULL) OR ([Source] = 2 AND [JobPositionId] IS NULL)");
                    table.ForeignKey(
                        name: "FK_EmployeeRoleAssignments_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "org",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeRoleAssignments_JobPositions_JobPositionId",
                        column: x => x.JobPositionId,
                        principalSchema: "org",
                        principalTable: "JobPositions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PositionRoles",
                schema: "org",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JobPositionId = table.Column<int>(type: "int", nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PositionRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PositionRoles_JobPositions_JobPositionId",
                        column: x => x.JobPositionId,
                        principalSchema: "org",
                        principalTable: "JobPositions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeRoleAssignments_EmployeeId_RoleId_Source",
                schema: "org",
                table: "EmployeeRoleAssignments",
                columns: new[] { "EmployeeId", "RoleId", "Source" },
                unique: true,
                filter: "[Source] = 2 AND [JobPositionId] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeRoleAssignments_EmployeeId_RoleId_Source_JobPositionId",
                schema: "org",
                table: "EmployeeRoleAssignments",
                columns: new[] { "EmployeeId", "RoleId", "Source", "JobPositionId" },
                unique: true,
                filter: "[Source] = 1 AND [JobPositionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeRoleAssignments_EmployeeId_Source",
                schema: "org",
                table: "EmployeeRoleAssignments",
                columns: new[] { "EmployeeId", "Source" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeRoleAssignments_JobPositionId",
                schema: "org",
                table: "EmployeeRoleAssignments",
                column: "JobPositionId");

            migrationBuilder.CreateIndex(
                name: "IX_PositionRoles_JobPositionId_RoleId",
                schema: "org",
                table: "PositionRoles",
                columns: new[] { "JobPositionId", "RoleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PositionRoles_RoleId",
                schema: "org",
                table: "PositionRoles",
                column: "RoleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmployeeRoleAssignments",
                schema: "org");

            migrationBuilder.DropTable(
                name: "PositionRoles",
                schema: "org");
        }
    }
}
