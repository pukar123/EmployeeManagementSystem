using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EMS.Domain.Database.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceRoleIdWithRoleKeyAndOutboxIdempotency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PositionRoles_JobPositionId_RoleId",
                schema: "org",
                table: "PositionRoles");

            migrationBuilder.DropIndex(
                name: "IX_PositionRoles_RoleId",
                schema: "org",
                table: "PositionRoles");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeRoleAssignments_EmployeeId_RoleId_Source",
                schema: "org",
                table: "EmployeeRoleAssignments");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeRoleAssignments_EmployeeId_RoleId_Source_JobPositionId",
                schema: "org",
                table: "EmployeeRoleAssignments");

            migrationBuilder.AddColumn<string>(
                name: "RoleKey",
                schema: "org",
                table: "PositionRoles",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RoleKey",
                schema: "org",
                table: "EmployeeRoleAssignments",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE org.PositionRoles SET RoleKey = CAST(RoleId AS nvarchar(128));
                UPDATE org.EmployeeRoleAssignments SET RoleKey = CAST(RoleId AS nvarchar(128));
                """);

            migrationBuilder.DropColumn(
                name: "RoleId",
                schema: "org",
                table: "PositionRoles");

            migrationBuilder.DropColumn(
                name: "RoleId",
                schema: "org",
                table: "EmployeeRoleAssignments");

            migrationBuilder.AlterColumn<string>(
                name: "RoleKey",
                schema: "org",
                table: "PositionRoles",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "RoleKey",
                schema: "org",
                table: "EmployeeRoleAssignments",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                schema: "ems",
                table: "IntegrationOutboxMessages",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE ems.IntegrationOutboxMessages
                SET IdempotencyKey = CONCAT('legacy:', CAST(Id AS nvarchar(32)));
                """);

            migrationBuilder.AlterColumn<string>(
                name: "IdempotencyKey",
                schema: "ems",
                table: "IntegrationOutboxMessages",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_PositionRoles_JobPositionId_RoleKey",
                schema: "org",
                table: "PositionRoles",
                columns: new[] { "JobPositionId", "RoleKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PositionRoles_RoleKey",
                schema: "org",
                table: "PositionRoles",
                column: "RoleKey");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationOutboxMessages_MessageType_IdempotencyKey",
                schema: "ems",
                table: "IntegrationOutboxMessages",
                columns: new[] { "MessageType", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeRoleAssignments_EmployeeId_RoleKey_Source",
                schema: "org",
                table: "EmployeeRoleAssignments",
                columns: new[] { "EmployeeId", "RoleKey", "Source" },
                unique: true,
                filter: "[Source] = 2 AND [JobPositionId] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeRoleAssignments_EmployeeId_RoleKey_Source_JobPositionId",
                schema: "org",
                table: "EmployeeRoleAssignments",
                columns: new[] { "EmployeeId", "RoleKey", "Source", "JobPositionId" },
                unique: true,
                filter: "[Source] = 1 AND [JobPositionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeRoleAssignments_RoleKey",
                schema: "org",
                table: "EmployeeRoleAssignments",
                column: "RoleKey");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PositionRoles_JobPositionId_RoleKey",
                schema: "org",
                table: "PositionRoles");

            migrationBuilder.DropIndex(
                name: "IX_PositionRoles_RoleKey",
                schema: "org",
                table: "PositionRoles");

            migrationBuilder.DropIndex(
                name: "IX_IntegrationOutboxMessages_MessageType_IdempotencyKey",
                schema: "ems",
                table: "IntegrationOutboxMessages");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeRoleAssignments_EmployeeId_RoleKey_Source",
                schema: "org",
                table: "EmployeeRoleAssignments");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeRoleAssignments_EmployeeId_RoleKey_Source_JobPositionId",
                schema: "org",
                table: "EmployeeRoleAssignments");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeRoleAssignments_RoleKey",
                schema: "org",
                table: "EmployeeRoleAssignments");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                schema: "ems",
                table: "IntegrationOutboxMessages");

            migrationBuilder.AddColumn<int>(
                name: "RoleId",
                schema: "org",
                table: "PositionRoles",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RoleId",
                schema: "org",
                table: "EmployeeRoleAssignments",
                type: "int",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE org.PositionRoles SET RoleId = TRY_CAST(RoleKey AS int) WHERE TRY_CAST(RoleKey AS int) IS NOT NULL;
                UPDATE org.EmployeeRoleAssignments SET RoleId = TRY_CAST(RoleKey AS int) WHERE TRY_CAST(RoleKey AS int) IS NOT NULL;
                UPDATE org.PositionRoles SET RoleId = 0 WHERE RoleId IS NULL;
                UPDATE org.EmployeeRoleAssignments SET RoleId = 0 WHERE RoleId IS NULL;
                """);

            migrationBuilder.DropColumn(
                name: "RoleKey",
                schema: "org",
                table: "PositionRoles");

            migrationBuilder.DropColumn(
                name: "RoleKey",
                schema: "org",
                table: "EmployeeRoleAssignments");

            migrationBuilder.AlterColumn<int>(
                name: "RoleId",
                schema: "org",
                table: "PositionRoles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "RoleId",
                schema: "org",
                table: "EmployeeRoleAssignments",
                type: "int",
                nullable: false,
                defaultValue: 0);

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
        }
    }
}
