using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EMS.Domain.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddOnboardingChecklistModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EmployeeOnboardingChecklistId",
                schema: "org",
                table: "Tasks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OnboardingTemplateItemId",
                schema: "org",
                table: "Tasks",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OnboardingChecklistTemplates",
                schema: "org",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganizationId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OnboardingChecklistTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OnboardingChecklistTemplates_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "org",
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeOnboardingChecklists",
                schema: "org",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    TemplateId = table.Column<int>(type: "int", nullable: false),
                    GeneratedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    GeneratedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeOnboardingChecklists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeOnboardingChecklists_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "org",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeOnboardingChecklists_OnboardingChecklistTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalSchema: "org",
                        principalTable: "OnboardingChecklistTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OnboardingChecklistTemplateItems",
                schema: "org",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TemplateId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Category = table.Column<int>(type: "int", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    DefaultDueDaysFromStart = table.Column<int>(type: "int", nullable: true),
                    DefaultPriority = table.Column<int>(type: "int", nullable: true),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OnboardingChecklistTemplateItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OnboardingChecklistTemplateItems_OnboardingChecklistTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalSchema: "org",
                        principalTable: "OnboardingChecklistTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_EmployeeOnboardingChecklistId",
                schema: "org",
                table: "Tasks",
                column: "EmployeeOnboardingChecklistId");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_OnboardingTemplateItemId",
                schema: "org",
                table: "Tasks",
                column: "OnboardingTemplateItemId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeOnboardingChecklists_EmployeeId",
                schema: "org",
                table: "EmployeeOnboardingChecklists",
                column: "EmployeeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeOnboardingChecklists_TemplateId",
                schema: "org",
                table: "EmployeeOnboardingChecklists",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingChecklistTemplateItems_TemplateId_SortOrder",
                schema: "org",
                table: "OnboardingChecklistTemplateItems",
                columns: new[] { "TemplateId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingChecklistTemplates_OrganizationId",
                schema: "org",
                table: "OnboardingChecklistTemplates",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingChecklistTemplates_OrganizationId_Name",
                schema: "org",
                table: "OnboardingChecklistTemplates",
                columns: new[] { "OrganizationId", "Name" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Tasks_EmployeeOnboardingChecklists_EmployeeOnboardingChecklistId",
                schema: "org",
                table: "Tasks",
                column: "EmployeeOnboardingChecklistId",
                principalSchema: "org",
                principalTable: "EmployeeOnboardingChecklists",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Tasks_OnboardingChecklistTemplateItems_OnboardingTemplateItemId",
                schema: "org",
                table: "Tasks",
                column: "OnboardingTemplateItemId",
                principalSchema: "org",
                principalTable: "OnboardingChecklistTemplateItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tasks_EmployeeOnboardingChecklists_EmployeeOnboardingChecklistId",
                schema: "org",
                table: "Tasks");

            migrationBuilder.DropForeignKey(
                name: "FK_Tasks_OnboardingChecklistTemplateItems_OnboardingTemplateItemId",
                schema: "org",
                table: "Tasks");

            migrationBuilder.DropTable(
                name: "EmployeeOnboardingChecklists",
                schema: "org");

            migrationBuilder.DropTable(
                name: "OnboardingChecklistTemplateItems",
                schema: "org");

            migrationBuilder.DropTable(
                name: "OnboardingChecklistTemplates",
                schema: "org");

            migrationBuilder.DropIndex(
                name: "IX_Tasks_EmployeeOnboardingChecklistId",
                schema: "org",
                table: "Tasks");

            migrationBuilder.DropIndex(
                name: "IX_Tasks_OnboardingTemplateItemId",
                schema: "org",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "EmployeeOnboardingChecklistId",
                schema: "org",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "OnboardingTemplateItemId",
                schema: "org",
                table: "Tasks");
        }
    }
}
