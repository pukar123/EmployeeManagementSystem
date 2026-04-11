using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EMS.Domain.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddSitesAndEmployeeSites : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Sites",
                schema: "org",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SiteName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SiteDescription = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    SiteLocation = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sites", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeSites",
                schema: "org",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    SiteId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeSites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeSites_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "org",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeSites_Sites_SiteId",
                        column: x => x.SiteId,
                        principalSchema: "org",
                        principalTable: "Sites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeSites_EmployeeId",
                schema: "org",
                table: "EmployeeSites",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeSites_EmployeeId_SiteId",
                schema: "org",
                table: "EmployeeSites",
                columns: new[] { "EmployeeId", "SiteId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeSites_SiteId",
                schema: "org",
                table: "EmployeeSites",
                column: "SiteId");

            migrationBuilder.CreateIndex(
                name: "IX_Sites_IsDeleted",
                schema: "org",
                table: "Sites",
                column: "IsDeleted");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmployeeSites",
                schema: "org");

            migrationBuilder.DropTable(
                name: "Sites",
                schema: "org");
        }
    }
}
