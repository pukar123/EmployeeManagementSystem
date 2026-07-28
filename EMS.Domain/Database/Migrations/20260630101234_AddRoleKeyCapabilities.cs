using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EMS.Domain.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddRoleKeyCapabilities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RoleKeyCapabilities",
                schema: "ems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CapabilityKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Allowed = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleKeyCapabilities", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RoleKeyCapabilities_RoleKey_CapabilityKey",
                schema: "ems",
                table: "RoleKeyCapabilities",
                columns: new[] { "RoleKey", "CapabilityKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RoleKeyCapabilities",
                schema: "ems");
        }
    }
}
