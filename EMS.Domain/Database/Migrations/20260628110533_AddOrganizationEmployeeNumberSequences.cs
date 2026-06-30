using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EMS.Domain.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationEmployeeNumberSequences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OrganizationEmployeeNumberSequences",
                schema: "org",
                columns: table => new
                {
                    OrganizationId = table.Column<int>(type: "int", nullable: false),
                    LastAllocatedNumber = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationEmployeeNumberSequences", x => x.OrganizationId);
                    table.ForeignKey(
                        name: "FK_OrganizationEmployeeNumberSequences_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "org",
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
            migrationBuilder.Sql(
                """
                INSERT INTO [org].[OrganizationEmployeeNumberSequences] ([OrganizationId], [LastAllocatedNumber])
                SELECT
                    e.[OrganizationId],
                    COALESCE(MAX(
                        CASE
                            WHEN UPPER(LTRIM(RTRIM(e.[EmployeeNumber]))) LIKE 'EMP[0-9]%'
                                AND PATINDEX('%[^0-9]%', SUBSTRING(UPPER(LTRIM(RTRIM(e.[EmployeeNumber]))), 4, 4000)) = 0
                            THEN TRY_CAST(SUBSTRING(UPPER(LTRIM(RTRIM(e.[EmployeeNumber]))), 4, 4000) AS int)
                            ELSE 0
                        END
                    ), 0) AS [LastAllocatedNumber]
                FROM [org].[Employees] e
                GROUP BY e.[OrganizationId];
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrganizationEmployeeNumberSequences",
                schema: "org");
        }
    }
}
