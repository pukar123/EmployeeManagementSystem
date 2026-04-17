using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EMS.Domain.Database.Migrations
{
    /// <inheritdoc />
    public partial class HistoricalTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ArchiveReason",
                schema: "org",
                table: "Employees",
                type: "nvarchar(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAtUtc",
                schema: "org",
                table: "Employees",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                schema: "org",
                table: "Employees",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "RetentionUntilUtc",
                schema: "org",
                table: "Employees",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AuditTrailEntries",
                schema: "org",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EntityName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    EntityKey = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    OldValuesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValuesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChangedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ActorUserId = table.Column<int>(type: "int", nullable: true),
                    ActorEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ActorUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditTrailEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeDepartmentHistories",
                schema: "org",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    PreviousDepartmentId = table.Column<int>(type: "int", nullable: true),
                    NewDepartmentId = table.Column<int>(type: "int", nullable: true),
                    EffectiveFromUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveToUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    ChangedByUserId = table.Column<int>(type: "int", nullable: true),
                    ChangedByUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ChangedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeDepartmentHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeDepartmentHistories_Departments_NewDepartmentId",
                        column: x => x.NewDepartmentId,
                        principalSchema: "org",
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeDepartmentHistories_Departments_PreviousDepartmentId",
                        column: x => x.PreviousDepartmentId,
                        principalSchema: "org",
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeDepartmentHistories_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "org",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeManagerHistories",
                schema: "org",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    PreviousManagerId = table.Column<int>(type: "int", nullable: true),
                    NewManagerId = table.Column<int>(type: "int", nullable: true),
                    EffectiveFromUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveToUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    ChangedByUserId = table.Column<int>(type: "int", nullable: true),
                    ChangedByUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ChangedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeManagerHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeManagerHistories_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "org",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeManagerHistories_Employees_NewManagerId",
                        column: x => x.NewManagerId,
                        principalSchema: "org",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeManagerHistories_Employees_PreviousManagerId",
                        column: x => x.PreviousManagerId,
                        principalSchema: "org",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmployeePositionHistories",
                schema: "org",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    PreviousJobPositionId = table.Column<int>(type: "int", nullable: true),
                    NewJobPositionId = table.Column<int>(type: "int", nullable: true),
                    EffectiveFromUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveToUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    ChangedByUserId = table.Column<int>(type: "int", nullable: true),
                    ChangedByUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ChangedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeePositionHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeePositionHistories_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "org",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeePositionHistories_JobPositions_NewJobPositionId",
                        column: x => x.NewJobPositionId,
                        principalSchema: "org",
                        principalTable: "JobPositions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeePositionHistories_JobPositions_PreviousJobPositionId",
                        column: x => x.PreviousJobPositionId,
                        principalSchema: "org",
                        principalTable: "JobPositions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeRetentionPolicies",
                schema: "org",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganizationId = table.Column<int>(type: "int", nullable: true),
                    RetentionDays = table.Column<int>(type: "int", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeRetentionPolicies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeRetentionPolicies_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "org",
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Employees_IsArchived",
                schema: "org",
                table: "Employees",
                column: "IsArchived");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_RetentionUntilUtc",
                schema: "org",
                table: "Employees",
                column: "RetentionUntilUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AuditTrailEntries_ActorUserId",
                schema: "org",
                table: "AuditTrailEntries",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditTrailEntries_ChangedAtUtc",
                schema: "org",
                table: "AuditTrailEntries",
                column: "ChangedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AuditTrailEntries_EntityKey",
                schema: "org",
                table: "AuditTrailEntries",
                column: "EntityKey");

            migrationBuilder.CreateIndex(
                name: "IX_AuditTrailEntries_EntityName",
                schema: "org",
                table: "AuditTrailEntries",
                column: "EntityName");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeDepartmentHistories_CreatedAtUtc",
                schema: "org",
                table: "EmployeeDepartmentHistories",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeDepartmentHistories_EffectiveFromUtc",
                schema: "org",
                table: "EmployeeDepartmentHistories",
                column: "EffectiveFromUtc");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeDepartmentHistories_EmployeeId",
                schema: "org",
                table: "EmployeeDepartmentHistories",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeDepartmentHistories_NewDepartmentId",
                schema: "org",
                table: "EmployeeDepartmentHistories",
                column: "NewDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeDepartmentHistories_PreviousDepartmentId",
                schema: "org",
                table: "EmployeeDepartmentHistories",
                column: "PreviousDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeManagerHistories_CreatedAtUtc",
                schema: "org",
                table: "EmployeeManagerHistories",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeManagerHistories_EffectiveFromUtc",
                schema: "org",
                table: "EmployeeManagerHistories",
                column: "EffectiveFromUtc");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeManagerHistories_EmployeeId",
                schema: "org",
                table: "EmployeeManagerHistories",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeManagerHistories_NewManagerId",
                schema: "org",
                table: "EmployeeManagerHistories",
                column: "NewManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeManagerHistories_PreviousManagerId",
                schema: "org",
                table: "EmployeeManagerHistories",
                column: "PreviousManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeePositionHistories_CreatedAtUtc",
                schema: "org",
                table: "EmployeePositionHistories",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeePositionHistories_EffectiveFromUtc",
                schema: "org",
                table: "EmployeePositionHistories",
                column: "EffectiveFromUtc");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeePositionHistories_EmployeeId",
                schema: "org",
                table: "EmployeePositionHistories",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeePositionHistories_NewJobPositionId",
                schema: "org",
                table: "EmployeePositionHistories",
                column: "NewJobPositionId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeePositionHistories_PreviousJobPositionId",
                schema: "org",
                table: "EmployeePositionHistories",
                column: "PreviousJobPositionId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeRetentionPolicies_OrganizationId",
                schema: "org",
                table: "EmployeeRetentionPolicies",
                column: "OrganizationId",
                unique: true,
                filter: "[OrganizationId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditTrailEntries",
                schema: "org");

            migrationBuilder.DropTable(
                name: "EmployeeDepartmentHistories",
                schema: "org");

            migrationBuilder.DropTable(
                name: "EmployeeManagerHistories",
                schema: "org");

            migrationBuilder.DropTable(
                name: "EmployeePositionHistories",
                schema: "org");

            migrationBuilder.DropTable(
                name: "EmployeeRetentionPolicies",
                schema: "org");

            migrationBuilder.DropIndex(
                name: "IX_Employees_IsArchived",
                schema: "org",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_Employees_RetentionUntilUtc",
                schema: "org",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "ArchiveReason",
                schema: "org",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "ArchivedAtUtc",
                schema: "org",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                schema: "org",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "RetentionUntilUtc",
                schema: "org",
                table: "Employees");
        }
    }
}
