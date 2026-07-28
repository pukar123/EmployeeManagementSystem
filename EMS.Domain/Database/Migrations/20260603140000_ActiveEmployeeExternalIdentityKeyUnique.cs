using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EMS.Domain.Database.Migrations
{
    /// <inheritdoc />
    public partial class ActiveEmployeeExternalIdentityKeyUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Employees_ExternalIdentityKey",
                schema: "org",
                table: "Employees",
                column: "ExternalIdentityKey",
                unique: true,
                filter: "[ExternalIdentityKey] IS NOT NULL AND [IsArchived] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Employees_ExternalIdentityKey",
                schema: "org",
                table: "Employees");
        }
    }
}
