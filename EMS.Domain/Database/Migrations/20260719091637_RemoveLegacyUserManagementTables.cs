using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EMS.Domain.Database.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLegacyUserManagementTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // EMS no longer owns User Management data. This cleanup is DESTRUCTIVE and
            // irreversible, so it is operator-gated: the legacy [um] tables are dropped
            // ONLY when an explicit approval marker table [emp].[_UmLegacyDropApproved]
            // exists. Without the marker the migration records itself as applied but
            // performs no drop, so it can never silently destroy authoritative data on
            // an unattended Development/Docker startup.
            //
            // To approve the drop AFTER database-split validation and a verified backup:
            //   CREATE TABLE [emp].[_UmLegacyDropApproved] (Id int NOT NULL PRIMARY KEY);
            //   INSERT INTO [emp].[_UmLegacyDropApproved] (Id) VALUES (1);
            // then run the EMS migrations (or a follow-up cleanup migration/script).
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[emp].[_UmLegacyDropApproved]', N'U') IS NULL
                BEGIN
                    PRINT 'RemoveLegacyUserManagementTables: approval marker [emp].[_UmLegacyDropApproved] not found. Skipping destructive [um] drop. Create the marker after split validation and backup to enable it.';
                END
                ELSE IF SCHEMA_ID(N'um') IS NOT NULL
                BEGIN
                    IF OBJECT_ID(N'[um].[PasswordResetTokens]', N'U') IS NOT NULL
                        DROP TABLE [um].[PasswordResetTokens];

                    IF OBJECT_ID(N'[um].[IdempotencyRecords]', N'U') IS NOT NULL
                        DROP TABLE [um].[IdempotencyRecords];

                    IF OBJECT_ID(N'[um].[AccountInvitations]', N'U') IS NOT NULL
                        DROP TABLE [um].[AccountInvitations];

                    IF OBJECT_ID(N'[um].[UserRoles]', N'U') IS NOT NULL
                        DROP TABLE [um].[UserRoles];

                    IF OBJECT_ID(N'[um].[RefreshTokens]', N'U') IS NOT NULL
                        DROP TABLE [um].[RefreshTokens];

                    IF OBJECT_ID(N'[um].[ServiceClients]', N'U') IS NOT NULL
                        DROP TABLE [um].[ServiceClients];

                    IF OBJECT_ID(N'[um].[Roles]', N'U') IS NOT NULL
                        DROP TABLE [um].[Roles];

                    IF OBJECT_ID(N'[um].[Users]', N'U') IS NOT NULL
                        DROP TABLE [um].[Users];

                    IF NOT EXISTS (
                        SELECT 1
                        FROM sys.objects
                        WHERE schema_id = SCHEMA_ID(N'um'))
                    BEGIN
                        EXEC(N'DROP SCHEMA [um]');
                    END
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new InvalidOperationException(
                "The legacy EMS User Management tables were intentionally dropped and cannot be restored by this migration. Restore EMSDevDB from backup if rollback is required.");
        }
    }
}
