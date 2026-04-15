using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EMS.Domain.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddRoleKeyPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RoleKeyPermissions",
                schema: "ems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    MenuId = table.Column<int>(type: "int", nullable: false),
                    Allowed = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleKeyPermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoleKeyPermissions_Menus_MenuId",
                        column: x => x.MenuId,
                        principalSchema: "ems",
                        principalTable: "Menus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RoleKeyPermissions_MenuId",
                schema: "ems",
                table: "RoleKeyPermissions",
                column: "MenuId");

            migrationBuilder.CreateIndex(
                name: "IX_RoleKeyPermissions_RoleKey_MenuId",
                schema: "ems",
                table: "RoleKeyPermissions",
                columns: new[] { "RoleKey", "MenuId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RoleKeyPermissions",
                schema: "ems");
        }
    }
}
