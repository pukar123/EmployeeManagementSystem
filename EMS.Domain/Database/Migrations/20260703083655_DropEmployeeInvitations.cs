using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EMS.Domain.Database.Migrations
{
    /// <inheritdoc />
    /// <summary>Phase A (additive): preserve emp.EmployeeInvitations until UmDbSplitMigrator copy + soak period.</summary>
    public partial class DropEmployeeInvitations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF NOT EXISTS (
                    SELECT 1 FROM sys.tables t
                    INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
                    WHERE s.name = N'emp' AND t.name = N'_UmSeparationCutoverMarker')
                BEGIN
                    CREATE TABLE [emp].[_UmSeparationCutoverMarker] (
                        [Id] int NOT NULL CONSTRAINT [PK_UmSeparationCutoverMarker] PRIMARY KEY,
                        [PreparedAtUtc] datetime2 NOT NULL CONSTRAINT [DF_UmSeparationCutoverMarker_PreparedAtUtc] DEFAULT (GETUTCDATE()),
                        CONSTRAINT [CK_UmSeparationCutoverMarker_Singleton] CHECK ([Id] = 1)
                    );
                    INSERT INTO [emp].[_UmSeparationCutoverMarker] ([Id]) VALUES (1);
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "_UmSeparationCutoverMarker",
                schema: "emp");
        }
    }
}
