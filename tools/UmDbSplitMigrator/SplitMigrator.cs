using System.Globalization;
using Microsoft.Data.SqlClient;

namespace UmDbSplitMigrator;

internal sealed class SplitMigrator
{
    public static readonly string[] UmMigrationIds =
    [
        "20260406124106_InitialUserManagement",
        "20260412091718_AddRolesAndUserRoles",
        "20260416141843_AddMustChangePassword",
        "20260703083505_AddAccountInvitationsAndServiceClients",
    ];

    private readonly SqlConnection _source;
    private readonly SqlConnection _target;
    private readonly bool _dryRun;

    public SplitMigrator(SqlConnection source, SqlConnection target, bool dryRun)
    {
        _source = source;
        _target = target;
        _dryRun = dryRun;
    }

    public async Task ApplyAsync(CancellationToken cancellationToken)
    {
        await EnsureApplyPrerequisitesAsync(cancellationToken);

        var plan = await BuildPlanAsync(cancellationToken);
        WritePlan(plan);

        if (_dryRun)
        {
            Console.WriteLine("Dry-run complete. No changes were written.");
            return;
        }

        await using var targetTx = (SqlTransaction)await _target.BeginTransactionAsync(cancellationToken);
        await using var sourceTx = (SqlTransaction)await _source.BeginTransactionAsync(cancellationToken);

        try
        {
            await EnsureLedgerTablesAsync(targetTx, sourceTx, cancellationToken);

            await CopyRolesAsync(targetTx, cancellationToken);
            await CopyUsersAsync(targetTx, cancellationToken);
            await CopyUserRolesAsync(targetTx, cancellationToken);
            await CopyRefreshTokensAsync(targetTx, cancellationToken);
            await SyncMigrationHistoryAsync(targetTx, cancellationToken);
            await MigrateInvitationsAsync(targetTx, cancellationToken);
            await BackfillRoleKeysAsync(sourceTx, cancellationToken);

            await SqlHelpers.ExecuteAsync(
                _target,
                targetTx,
                """
                INSERT INTO um._SplitMigrationLedger (AppliedAtUtc, Mode, Notes)
                VALUES (SYSUTCDATETIME(), 'apply', 'UM data split migration applied')
                """,
                cancellationToken);

            await targetTx.CommitAsync(cancellationToken);
            await sourceTx.CommitAsync(cancellationToken);
            Console.WriteLine("Apply completed successfully. Source um.* tables were left intact.");
        }
        catch
        {
            await targetTx.RollbackAsync(cancellationToken);
            await sourceTx.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task RollbackAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("Rollback removes migrated rows from UserManagementDb and restores EMS RoleKey snapshots.");
        Console.WriteLine("Source EMS um.* tables are never deleted.");

        await using var targetTx = (SqlTransaction)await _target.BeginTransactionAsync(cancellationToken);
        await using var sourceTx = (SqlTransaction)await _source.BeginTransactionAsync(cancellationToken);

        try
        {
            var restoredKeys = await RestoreRoleKeySnapshotsAsync(sourceTx, cancellationToken);
            var deletedInvites = await DeleteMigratedInvitationsAsync(targetTx, cancellationToken);
            var deletedTokens = await DeleteCopiedByIdAsync(
                targetTx,
                "um.RefreshTokens",
                "SELECT Id FROM um.RefreshTokens",
                cancellationToken);
            var deletedUserRoles = await DeleteCopiedUserRolesAsync(targetTx, cancellationToken);
            var deletedUsers = await DeleteCopiedByIdAsync(
                targetTx,
                "um.Users",
                "SELECT Id FROM um.Users",
                cancellationToken);
            var deletedRoles = await DeleteCopiedByIdAsync(
                targetTx,
                "um.Roles",
                "SELECT Id FROM um.Roles",
                cancellationToken);

            if (await SqlHelpers.TableExistsAsync(_target, "um", "_SplitMigrationLedger", cancellationToken))
            {
                await SqlHelpers.ExecuteAsync(
                    _target,
                    targetTx,
                    """
                    INSERT INTO um._SplitMigrationLedger (AppliedAtUtc, Mode, Notes)
                    VALUES (SYSUTCDATETIME(), 'rollback', @notes)
                    """,
                    cancellationToken,
                    ("@notes",
                        $"restoredRoleKeys={restoredKeys}; invites={deletedInvites}; tokens={deletedTokens}; userRoles={deletedUserRoles}; users={deletedUsers}; roles={deletedRoles}"));
            }

            await targetTx.CommitAsync(cancellationToken);
            await sourceTx.CommitAsync(cancellationToken);

            Console.WriteLine(
                $"Rollback completed. restoredRoleKeys={restoredKeys}, invites={deletedInvites}, tokens={deletedTokens}, userRoles={deletedUserRoles}, users={deletedUsers}, roles={deletedRoles}");
            Console.WriteLine("Restore databases from backup if you need a full environment reset.");
        }
        catch
        {
            await targetTx.RollbackAsync(cancellationToken);
            await sourceTx.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task EnsureApplyPrerequisitesAsync(CancellationToken cancellationToken)
    {
        foreach (var table in new[] { "Users", "Roles", "UserRoles", "RefreshTokens" })
        {
            if (!await SqlHelpers.TableExistsAsync(_source, "um", table, cancellationToken))
                throw new InvalidOperationException($"Source is missing um.{table}.");
            if (!await SqlHelpers.TableExistsAsync(_target, "um", table, cancellationToken))
                throw new InvalidOperationException(
                    $"Target is missing um.{table}. Create UserManagementDb and apply UM EF migrations first.");
        }

        if (!await SqlHelpers.TableExistsAsync(_target, "um", "AccountInvitations", cancellationToken))
            throw new InvalidOperationException("Target is missing um.AccountInvitations. Apply latest UM EF migrations first.");
    }

    private async Task<MigrationPlan> BuildPlanAsync(CancellationToken cancellationToken)
    {
        var plan = new MigrationPlan
        {
            RolesToInsert = await CountMissingIdsAsync("um.Roles", cancellationToken),
            UsersToInsert = await CountMissingIdsAsync("um.Users", cancellationToken),
            UserRolesToInsert = await CountMissingUserRolesAsync(cancellationToken),
            RefreshTokensToInsert = await CountMissingIdsAsync("um.RefreshTokens", cancellationToken),
            MigrationHistoryToInsert = await CountMissingMigrationHistoryAsync(cancellationToken),
            InvitationsToInsert = await CountMissingInvitationsAsync(cancellationToken),
            RoleKeysToBackfill = await CountLegacyRoleKeysAsync(cancellationToken),
        };
        return plan;
    }

    private static void WritePlan(MigrationPlan plan)
    {
        Console.WriteLine("Migration plan");
        Console.WriteLine(new string('-', 48));
        Console.WriteLine($"Roles to insert/update:            {plan.RolesToInsert}");
        Console.WriteLine($"Users to insert/update:            {plan.UsersToInsert}");
        Console.WriteLine($"UserRoles to insert:               {plan.UserRolesToInsert}");
        Console.WriteLine($"RefreshTokens to insert/update:    {plan.RefreshTokensToInsert}");
        Console.WriteLine($"EF migration history rows:         {plan.MigrationHistoryToInsert}");
        Console.WriteLine($"Invitations to insert:             {plan.InvitationsToInsert}");
        Console.WriteLine($"Legacy RoleKey rows to backfill:   {plan.RoleKeysToBackfill}");
        Console.WriteLine(new string('-', 48));
    }

    private async Task EnsureLedgerTablesAsync(
        SqlTransaction targetTx,
        SqlTransaction sourceTx,
        CancellationToken cancellationToken)
    {
        await SqlHelpers.ExecuteAsync(
            _target,
            targetTx,
            """
            IF OBJECT_ID(N'um._SplitMigrationLedger', N'U') IS NULL
            BEGIN
                CREATE TABLE um._SplitMigrationLedger
                (
                    Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    AppliedAtUtc datetime2 NOT NULL,
                    Mode nvarchar(32) NOT NULL,
                    Notes nvarchar(max) NULL
                );
            END
            """,
            cancellationToken);

        await SqlHelpers.ExecuteAsync(
            _source,
            sourceTx,
            """
            IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'ems')
                EXEC(N'CREATE SCHEMA ems');

            IF OBJECT_ID(N'ems._RoleKeyBackfillSnapshot', N'U') IS NULL
            BEGIN
                CREATE TABLE ems._RoleKeyBackfillSnapshot
                (
                    TableName nvarchar(64) NOT NULL,
                    RowId int NOT NULL,
                    PreviousRoleKey nvarchar(128) NOT NULL,
                    NewRoleKey nvarchar(128) NOT NULL,
                    AppliedAtUtc datetime2 NOT NULL,
                    CONSTRAINT PK_RoleKeyBackfillSnapshot PRIMARY KEY (TableName, RowId)
                );
            END
            """,
            cancellationToken);
    }

    private async Task CopyRolesAsync(SqlTransaction targetTx, CancellationToken cancellationToken)
    {
        await using var read = _source.CreateCommand();
        read.CommandText = "SELECT Id, Name, NormalizedName, Description, IsSystem FROM um.Roles ORDER BY Id";
        await SqlHelpers.ExecuteAsync(_target, targetTx, "SET IDENTITY_INSERT um.Roles ON", cancellationToken);
        try
        {
            await using var reader = await read.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                await SqlHelpers.ExecuteAsync(
                    _target,
                    targetTx,
                    """
                    MERGE um.Roles AS t
                    USING (SELECT @Id AS Id) AS s ON t.Id = s.Id
                    WHEN MATCHED THEN
                        UPDATE SET
                            Name = @Name,
                            NormalizedName = @NormalizedName,
                            Description = @Description,
                            IsSystem = @IsSystem
                    WHEN NOT MATCHED THEN
                        INSERT (Id, Name, NormalizedName, Description, IsSystem)
                        VALUES (@Id, @Name, @NormalizedName, @Description, @IsSystem);
                    """,
                    cancellationToken,
                    ("@Id", reader.GetInt32(0)),
                    ("@Name", reader.GetString(1)),
                    ("@NormalizedName", reader.GetString(2)),
                    ("@Description", reader.IsDBNull(3) ? null : reader.GetString(3)),
                    ("@IsSystem", reader.GetBoolean(4)));
            }
        }
        finally
        {
            await SqlHelpers.ExecuteAsync(_target, targetTx, "SET IDENTITY_INSERT um.Roles OFF", cancellationToken);
            await SqlHelpers.ReseedIdentityAsync(_target, targetTx, "um.Roles", cancellationToken);
        }
    }

    private async Task CopyUsersAsync(SqlTransaction targetTx, CancellationToken cancellationToken)
    {
        var sourceHasMustChange = await SqlHelpers.ColumnExistsAsync(_source, "um", "Users", "MustChangePassword", cancellationToken);
        var targetHasMustChange = await SqlHelpers.ColumnExistsAsync(_target, "um", "Users", "MustChangePassword", cancellationToken);

        await using var read = _source.CreateCommand();
        read.CommandText = sourceHasMustChange
            ? """
              SELECT Id, Email, NormalizedEmail, PasswordHash, UserName, IsActive, MustChangePassword, CreatedAtUtc, LastLoginAtUtc
              FROM um.Users
              ORDER BY Id
              """
            : """
              SELECT Id, Email, NormalizedEmail, PasswordHash, UserName, IsActive, CAST(0 AS bit) AS MustChangePassword, CreatedAtUtc, LastLoginAtUtc
              FROM um.Users
              ORDER BY Id
              """;

        await SqlHelpers.ExecuteAsync(_target, targetTx, "SET IDENTITY_INSERT um.Users ON", cancellationToken);
        try
        {
            await using var reader = await read.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                if (targetHasMustChange)
                {
                    await SqlHelpers.ExecuteAsync(
                        _target,
                        targetTx,
                        """
                        MERGE um.Users AS t
                        USING (SELECT @Id AS Id) AS s ON t.Id = s.Id
                        WHEN MATCHED THEN
                            UPDATE SET
                                Email = @Email,
                                NormalizedEmail = @NormalizedEmail,
                                PasswordHash = @PasswordHash,
                                UserName = @UserName,
                                IsActive = @IsActive,
                                MustChangePassword = @MustChangePassword,
                                CreatedAtUtc = @CreatedAtUtc,
                                LastLoginAtUtc = @LastLoginAtUtc
                        WHEN NOT MATCHED THEN
                            INSERT (Id, Email, NormalizedEmail, PasswordHash, UserName, IsActive, MustChangePassword, CreatedAtUtc, LastLoginAtUtc)
                            VALUES (@Id, @Email, @NormalizedEmail, @PasswordHash, @UserName, @IsActive, @MustChangePassword, @CreatedAtUtc, @LastLoginAtUtc);
                        """,
                        cancellationToken,
                        ("@Id", reader.GetInt32(0)),
                        ("@Email", reader.GetString(1)),
                        ("@NormalizedEmail", reader.GetString(2)),
                        ("@PasswordHash", reader.GetString(3)),
                        ("@UserName", reader.IsDBNull(4) ? null : reader.GetString(4)),
                        ("@IsActive", reader.GetBoolean(5)),
                        ("@MustChangePassword", reader.GetBoolean(6)),
                        ("@CreatedAtUtc", reader.GetDateTime(7)),
                        ("@LastLoginAtUtc", reader.IsDBNull(8) ? null : reader.GetDateTime(8)));
                }
                else
                {
                    await SqlHelpers.ExecuteAsync(
                        _target,
                        targetTx,
                        """
                        MERGE um.Users AS t
                        USING (SELECT @Id AS Id) AS s ON t.Id = s.Id
                        WHEN MATCHED THEN
                            UPDATE SET
                                Email = @Email,
                                NormalizedEmail = @NormalizedEmail,
                                PasswordHash = @PasswordHash,
                                UserName = @UserName,
                                IsActive = @IsActive,
                                CreatedAtUtc = @CreatedAtUtc,
                                LastLoginAtUtc = @LastLoginAtUtc
                        WHEN NOT MATCHED THEN
                            INSERT (Id, Email, NormalizedEmail, PasswordHash, UserName, IsActive, CreatedAtUtc, LastLoginAtUtc)
                            VALUES (@Id, @Email, @NormalizedEmail, @PasswordHash, @UserName, @IsActive, @CreatedAtUtc, @LastLoginAtUtc);
                        """,
                        cancellationToken,
                        ("@Id", reader.GetInt32(0)),
                        ("@Email", reader.GetString(1)),
                        ("@NormalizedEmail", reader.GetString(2)),
                        ("@PasswordHash", reader.GetString(3)),
                        ("@UserName", reader.IsDBNull(4) ? null : reader.GetString(4)),
                        ("@IsActive", reader.GetBoolean(5)),
                        ("@CreatedAtUtc", reader.GetDateTime(7)),
                        ("@LastLoginAtUtc", reader.IsDBNull(8) ? null : reader.GetDateTime(8)));
                }
            }
        }
        finally
        {
            await SqlHelpers.ExecuteAsync(_target, targetTx, "SET IDENTITY_INSERT um.Users OFF", cancellationToken);
            await SqlHelpers.ReseedIdentityAsync(_target, targetTx, "um.Users", cancellationToken);
        }
    }

    private async Task CopyUserRolesAsync(SqlTransaction targetTx, CancellationToken cancellationToken)
    {
        await using var read = _source.CreateCommand();
        read.CommandText = "SELECT UserId, RoleId FROM um.UserRoles";
        await using var reader = await read.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            await SqlHelpers.ExecuteAsync(
                _target,
                targetTx,
                """
                IF NOT EXISTS (SELECT 1 FROM um.UserRoles WHERE UserId = @UserId AND RoleId = @RoleId)
                    INSERT INTO um.UserRoles (UserId, RoleId) VALUES (@UserId, @RoleId);
                """,
                cancellationToken,
                ("@UserId", reader.GetInt32(0)),
                ("@RoleId", reader.GetInt32(1)));
        }
    }

    private async Task CopyRefreshTokensAsync(SqlTransaction targetTx, CancellationToken cancellationToken)
    {
        await using var read = _source.CreateCommand();
        read.CommandText =
            """
            SELECT Id, UserId, TokenHash, ExpiresAtUtc, CreatedAtUtc, RevokedAtUtc, ClientInfo
            FROM um.RefreshTokens
            ORDER BY Id
            """;
        await SqlHelpers.ExecuteAsync(_target, targetTx, "SET IDENTITY_INSERT um.RefreshTokens ON", cancellationToken);
        try
        {
            await using var reader = await read.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                await SqlHelpers.ExecuteAsync(
                    _target,
                    targetTx,
                    """
                    MERGE um.RefreshTokens AS t
                    USING (SELECT @Id AS Id) AS s ON t.Id = s.Id
                    WHEN MATCHED THEN
                        UPDATE SET
                            UserId = @UserId,
                            TokenHash = @TokenHash,
                            ExpiresAtUtc = @ExpiresAtUtc,
                            CreatedAtUtc = @CreatedAtUtc,
                            RevokedAtUtc = @RevokedAtUtc,
                            ClientInfo = @ClientInfo
                    WHEN NOT MATCHED THEN
                        INSERT (Id, UserId, TokenHash, ExpiresAtUtc, CreatedAtUtc, RevokedAtUtc, ClientInfo)
                        VALUES (@Id, @UserId, @TokenHash, @ExpiresAtUtc, @CreatedAtUtc, @RevokedAtUtc, @ClientInfo);
                    """,
                    cancellationToken,
                    ("@Id", reader.GetInt32(0)),
                    ("@UserId", reader.GetInt32(1)),
                    ("@TokenHash", reader.GetString(2)),
                    ("@ExpiresAtUtc", reader.GetDateTime(3)),
                    ("@CreatedAtUtc", reader.GetDateTime(4)),
                    ("@RevokedAtUtc", reader.IsDBNull(5) ? null : reader.GetDateTime(5)),
                    ("@ClientInfo", reader.IsDBNull(6) ? null : reader.GetString(6)));
            }
        }
        finally
        {
            await SqlHelpers.ExecuteAsync(_target, targetTx, "SET IDENTITY_INSERT um.RefreshTokens OFF", cancellationToken);
            await SqlHelpers.ReseedIdentityAsync(_target, targetTx, "um.RefreshTokens", cancellationToken);
        }
    }

    private async Task SyncMigrationHistoryAsync(SqlTransaction targetTx, CancellationToken cancellationToken)
    {
        if (!await SqlHelpers.TableExistsAsync(_target, "dbo", "__EFMigrationsHistory", cancellationToken))
        {
            await SqlHelpers.ExecuteAsync(
                _target,
                targetTx,
                """
                CREATE TABLE dbo.__EFMigrationsHistory
                (
                    MigrationId nvarchar(150) NOT NULL PRIMARY KEY,
                    ProductVersion nvarchar(32) NOT NULL
                );
                """,
                cancellationToken);
        }

        var sourceHistory = new Dictionary<string, string>(StringComparer.Ordinal);
        if (await SqlHelpers.TableExistsAsync(_source, "dbo", "__EFMigrationsHistory", cancellationToken))
        {
            await using var read = _source.CreateCommand();
            read.CommandText = "SELECT MigrationId, ProductVersion FROM dbo.__EFMigrationsHistory";
            await using var reader = await read.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
                sourceHistory[reader.GetString(0)] = reader.GetString(1);
        }

        foreach (var migrationId in UmMigrationIds)
        {
            var productVersion = sourceHistory.TryGetValue(migrationId, out var version) ? version : "9.0.4";
            await SqlHelpers.ExecuteAsync(
                _target,
                targetTx,
                """
                IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = @MigrationId)
                    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
                    VALUES (@MigrationId, @ProductVersion);
                """,
                cancellationToken,
                ("@MigrationId", migrationId),
                ("@ProductVersion", productVersion));
        }
    }

    private async Task MigrateInvitationsAsync(SqlTransaction targetTx, CancellationToken cancellationToken)
    {
        if (!await SqlHelpers.TableExistsAsync(_source, "emp", "EmployeeInvitations", cancellationToken))
        {
            Console.WriteLine("emp.EmployeeInvitations not found on source; invitation copy skipped.");
            return;
        }

        await using var read = _source.CreateCommand();
        read.CommandText =
            """
            SELECT
                i.Id,
                i.EmployeeId,
                i.UserId,
                i.TokenHash,
                i.ExpiresAtUtc,
                i.UsedAtUtc,
                i.RevokedAtUtc,
                i.CreatedAtUtc,
                i.LastSentAtUtc,
                i.DeliveryStatus,
                i.DeliveryFailureReason,
                e.Email,
                e.FirstName,
                e.LastName
            FROM emp.EmployeeInvitations i
            INNER JOIN org.Employees e ON e.Id = i.EmployeeId
            WHERE i.UsedAtUtc IS NULL
              AND i.RevokedAtUtc IS NULL
              AND i.ExpiresAtUtc > SYSUTCDATETIME()
            ORDER BY i.Id
            """;

        await using var reader = await read.ExecuteReaderAsync(cancellationToken);
        var inserted = 0;
        while (await reader.ReadAsync(cancellationToken))
        {
            var employeeId = reader.GetInt32(1);
            var correlationId = $"employee:{employeeId.ToString(CultureInfo.InvariantCulture)}";
            var email = reader.GetString(11);
            var displayName = $"{reader.GetString(12)} {reader.GetString(13)}".Trim();

            var rows = await SqlHelpers.ExecuteAsync(
                _target,
                targetTx,
                """
                IF NOT EXISTS (
                    SELECT 1
                    FROM um.AccountInvitations
                    WHERE TokenHash = @TokenHash
                       OR (ExternalCorrelationId = @ExternalCorrelationId AND UsedAtUtc IS NULL AND RevokedAtUtc IS NULL)
                )
                BEGIN
                    INSERT INTO um.AccountInvitations
                    (
                        UserId,
                        ExternalCorrelationId,
                        RecipientEmail,
                        RecipientDisplayName,
                        TokenHash,
                        ExpiresAtUtc,
                        UsedAtUtc,
                        RevokedAtUtc,
                        CreatedAtUtc,
                        LastSentAtUtc,
                        DeliveryStatus,
                        DeliveryFailureReason
                    )
                    VALUES
                    (
                        @UserId,
                        @ExternalCorrelationId,
                        @RecipientEmail,
                        @RecipientDisplayName,
                        @TokenHash,
                        @ExpiresAtUtc,
                        @UsedAtUtc,
                        @RevokedAtUtc,
                        @CreatedAtUtc,
                        @LastSentAtUtc,
                        @DeliveryStatus,
                        @DeliveryFailureReason
                    );
                END
                """,
                cancellationToken,
                ("@UserId", reader.GetInt32(2)),
                ("@ExternalCorrelationId", correlationId),
                ("@RecipientEmail", email),
                ("@RecipientDisplayName", string.IsNullOrWhiteSpace(displayName) ? null : displayName),
                ("@TokenHash", reader.GetString(3)),
                ("@ExpiresAtUtc", reader.GetDateTime(4)),
                ("@UsedAtUtc", reader.IsDBNull(5) ? null : reader.GetDateTime(5)),
                ("@RevokedAtUtc", reader.IsDBNull(6) ? null : reader.GetDateTime(6)),
                ("@CreatedAtUtc", reader.GetDateTime(7)),
                ("@LastSentAtUtc", reader.IsDBNull(8) ? null : reader.GetDateTime(8)),
                ("@DeliveryStatus", reader.GetInt32(9)),
                ("@DeliveryFailureReason", reader.IsDBNull(10) ? null : reader.GetString(10)));

            if (rows > 0)
                inserted++;
        }

        Console.WriteLine($"Invitations migrated (active only): {inserted}");
    }

    private async Task BackfillRoleKeysAsync(SqlTransaction sourceTx, CancellationToken cancellationToken)
    {
        var roleMap = new Dictionary<string, string>(StringComparer.Ordinal);
        await using var roles = _source.CreateCommand();
        roles.Transaction = sourceTx;
        roles.CommandText = "SELECT Id, NormalizedName FROM um.Roles";
        await using (var reader = await roles.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
                roleMap[reader.GetInt32(0).ToString(CultureInfo.InvariantCulture)] = reader.GetString(1);
        }

        await BackfillTableRoleKeysAsync(
            sourceTx,
            "org.PositionRoles",
            "PositionRoles",
            roleMap,
            cancellationToken);
        await BackfillTableRoleKeysAsync(
            sourceTx,
            "org.EmployeeRoleAssignments",
            "EmployeeRoleAssignments",
            roleMap,
            cancellationToken);
    }

    private async Task BackfillTableRoleKeysAsync(
        SqlTransaction sourceTx,
        string schemaTable,
        string snapshotTableName,
        IReadOnlyDictionary<string, string> roleMap,
        CancellationToken cancellationToken)
    {
        var parts = schemaTable.Split('.');
        var schema = parts[0];
        var table = parts[1];
        if (!await SqlHelpers.TableExistsAsync(_source, schema, table, cancellationToken))
            return;

        if (!await SqlHelpers.ColumnExistsAsync(_source, schema, table, "RoleKey", cancellationToken))
            return;

        await using var read = _source.CreateCommand();
        read.Transaction = sourceTx;
        read.CommandText = $"SELECT Id, RoleKey FROM {schemaTable} WHERE RoleKey LIKE '[0-9]%'";
        var updates = new List<(int Id, string Previous, string Next)>();
        await using (var reader = await read.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                var id = reader.GetInt32(0);
                var previous = reader.GetString(1);
                if (!roleMap.TryGetValue(previous, out var next))
                    throw new InvalidOperationException(
                        $"{schemaTable} row {id} references unknown role id '{previous}'.");
                updates.Add((id, previous, next));
            }
        }

        foreach (var (id, previous, next) in updates)
        {
            await SqlHelpers.ExecuteAsync(
                _source,
                sourceTx,
                """
                IF NOT EXISTS (
                    SELECT 1 FROM ems._RoleKeyBackfillSnapshot
                    WHERE TableName = @TableName AND RowId = @RowId
                )
                BEGIN
                    INSERT INTO ems._RoleKeyBackfillSnapshot (TableName, RowId, PreviousRoleKey, NewRoleKey, AppliedAtUtc)
                    VALUES (@TableName, @RowId, @PreviousRoleKey, @NewRoleKey, SYSUTCDATETIME());
                END
                """,
                cancellationToken,
                ("@TableName", snapshotTableName),
                ("@RowId", id),
                ("@PreviousRoleKey", previous),
                ("@NewRoleKey", next));

            await SqlHelpers.ExecuteAsync(
                _source,
                sourceTx,
                $"UPDATE {schemaTable} SET RoleKey = @RoleKey WHERE Id = @Id AND RoleKey = @PreviousRoleKey",
                cancellationToken,
                ("@RoleKey", next),
                ("@Id", id),
                ("@PreviousRoleKey", previous));
        }

        Console.WriteLine($"RoleKey backfill on {schemaTable}: {updates.Count} row(s).");
    }

    private async Task<int> RestoreRoleKeySnapshotsAsync(SqlTransaction sourceTx, CancellationToken cancellationToken)
    {
        if (!await SqlHelpers.TableExistsAsync(_source, "ems", "_RoleKeyBackfillSnapshot", cancellationToken))
            return 0;

        await using var read = _source.CreateCommand();
        read.Transaction = sourceTx;
        read.CommandText = "SELECT TableName, RowId, PreviousRoleKey FROM ems._RoleKeyBackfillSnapshot";
        var rows = new List<(string TableName, int RowId, string Previous)>();
        await using (var reader = await read.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
                rows.Add((reader.GetString(0), reader.GetInt32(1), reader.GetString(2)));
        }

        foreach (var (tableName, rowId, previous) in rows)
        {
            var schemaTable = tableName switch
            {
                "PositionRoles" => "org.PositionRoles",
                "EmployeeRoleAssignments" => "org.EmployeeRoleAssignments",
                _ => throw new InvalidOperationException($"Unknown snapshot table '{tableName}'."),
            };

            await SqlHelpers.ExecuteAsync(
                _source,
                sourceTx,
                $"UPDATE {schemaTable} SET RoleKey = @PreviousRoleKey WHERE Id = @RowId",
                cancellationToken,
                ("@PreviousRoleKey", previous),
                ("@RowId", rowId));
        }

        await SqlHelpers.ExecuteAsync(
            _source,
            sourceTx,
            "DELETE FROM ems._RoleKeyBackfillSnapshot",
            cancellationToken);

        return rows.Count;
    }

    private async Task<int> DeleteMigratedInvitationsAsync(SqlTransaction targetTx, CancellationToken cancellationToken)
    {
        if (!await SqlHelpers.TableExistsAsync(_target, "um", "AccountInvitations", cancellationToken))
            return 0;

        return await SqlHelpers.ExecuteAsync(
            _target,
            targetTx,
            """
            DELETE FROM um.AccountInvitations
            WHERE ExternalCorrelationId LIKE 'employee:%'
            """,
            cancellationToken);
    }

    private async Task<int> DeleteCopiedByIdAsync(
        SqlTransaction targetTx,
        string targetTable,
        string sourceIdSql,
        CancellationToken cancellationToken)
    {
        var sourceIds = new List<int>();
        await using (var read = _source.CreateCommand())
        {
            read.CommandText = sourceIdSql;
            await using var reader = await read.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
                sourceIds.Add(reader.GetInt32(0));
        }

        var deleted = 0;
        foreach (var id in sourceIds)
        {
            deleted += await SqlHelpers.ExecuteAsync(
                _target,
                targetTx,
                $"DELETE FROM {targetTable} WHERE Id = @Id",
                cancellationToken,
                ("@Id", id));
        }

        return deleted;
    }

    private async Task<int> DeleteCopiedUserRolesAsync(SqlTransaction targetTx, CancellationToken cancellationToken)
    {
        var pairs = new List<(int UserId, int RoleId)>();
        await using (var read = _source.CreateCommand())
        {
            read.CommandText = "SELECT UserId, RoleId FROM um.UserRoles";
            await using var reader = await read.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
                pairs.Add((reader.GetInt32(0), reader.GetInt32(1)));
        }

        var deleted = 0;
        foreach (var (userId, roleId) in pairs)
        {
            deleted += await SqlHelpers.ExecuteAsync(
                _target,
                targetTx,
                "DELETE FROM um.UserRoles WHERE UserId = @UserId AND RoleId = @RoleId",
                cancellationToken,
                ("@UserId", userId),
                ("@RoleId", roleId));
        }

        return deleted;
    }

    private async Task<int> CountMissingIdsAsync(string schemaTable, CancellationToken cancellationToken)
    {
        var sourceIds = await ReadIdsAsync(_source, $"SELECT Id FROM {schemaTable}", cancellationToken);
        var targetIds = await ReadIdsAsync(_target, $"SELECT Id FROM {schemaTable}", cancellationToken);
        return sourceIds.Count(id => !targetIds.Contains(id));
    }

    private async Task<int> CountMissingUserRolesAsync(CancellationToken cancellationToken)
    {
        var source = await ReadUserRolesAsync(_source, cancellationToken);
        var target = await ReadUserRolesAsync(_target, cancellationToken);
        return source.Count(x => !target.Contains(x));
    }

    private async Task<int> CountMissingMigrationHistoryAsync(CancellationToken cancellationToken)
    {
        var target = await ReadMigrationIdsAsync(_target, cancellationToken);
        return UmMigrationIds.Count(id => !target.Contains(id));
    }

    private async Task<int> CountMissingInvitationsAsync(CancellationToken cancellationToken)
    {
        if (!await SqlHelpers.TableExistsAsync(_source, "emp", "EmployeeInvitations", cancellationToken))
            return 0;

        var sourcePending = (int)await SqlHelpers.CountAsync(
            _source,
            """
            SELECT COUNT(*)
            FROM emp.EmployeeInvitations
            WHERE UsedAtUtc IS NULL
              AND RevokedAtUtc IS NULL
              AND ExpiresAtUtc > SYSUTCDATETIME()
            """,
            cancellationToken);

        var targetPending = (int)await SqlHelpers.CountAsync(
            _target,
            """
            SELECT COUNT(*)
            FROM um.AccountInvitations
            WHERE ExternalCorrelationId LIKE 'employee:%'
              AND UsedAtUtc IS NULL
              AND RevokedAtUtc IS NULL
              AND ExpiresAtUtc > SYSUTCDATETIME()
            """,
            cancellationToken);

        return Math.Max(0, sourcePending - targetPending);
    }

    private async Task<int> CountLegacyRoleKeysAsync(CancellationToken cancellationToken)
    {
        var total = 0;
        if (await SqlHelpers.TableExistsAsync(_source, "org", "PositionRoles", cancellationToken)
            && await SqlHelpers.ColumnExistsAsync(_source, "org", "PositionRoles", "RoleKey", cancellationToken))
        {
            total += (int)await SqlHelpers.CountAsync(
                _source,
                "SELECT COUNT(*) FROM org.PositionRoles WHERE RoleKey LIKE '[0-9]%'",
                cancellationToken);
        }

        if (await SqlHelpers.TableExistsAsync(_source, "org", "EmployeeRoleAssignments", cancellationToken)
            && await SqlHelpers.ColumnExistsAsync(_source, "org", "EmployeeRoleAssignments", "RoleKey", cancellationToken))
        {
            total += (int)await SqlHelpers.CountAsync(
                _source,
                "SELECT COUNT(*) FROM org.EmployeeRoleAssignments WHERE RoleKey LIKE '[0-9]%'",
                cancellationToken);
        }

        return total;
    }

    private static async Task<HashSet<int>> ReadIdsAsync(
        SqlConnection connection,
        string sql,
        CancellationToken cancellationToken)
    {
        var ids = new HashSet<int>();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            ids.Add(reader.GetInt32(0));
        return ids;
    }

    private static async Task<HashSet<(int UserId, int RoleId)>> ReadUserRolesAsync(
        SqlConnection connection,
        CancellationToken cancellationToken)
    {
        var set = new HashSet<(int, int)>();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT UserId, RoleId FROM um.UserRoles";
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            set.Add((reader.GetInt32(0), reader.GetInt32(1)));
        return set;
    }

    private static async Task<HashSet<string>> ReadMigrationIdsAsync(
        SqlConnection connection,
        CancellationToken cancellationToken)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        if (!await SqlHelpers.TableExistsAsync(connection, "dbo", "__EFMigrationsHistory", cancellationToken))
            return ids;

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT MigrationId FROM dbo.__EFMigrationsHistory";
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            ids.Add(reader.GetString(0));
        return ids;
    }

    private sealed class MigrationPlan
    {
        public int RolesToInsert { get; init; }
        public int UsersToInsert { get; init; }
        public int UserRolesToInsert { get; init; }
        public int RefreshTokensToInsert { get; init; }
        public int MigrationHistoryToInsert { get; init; }
        public int InvitationsToInsert { get; init; }
        public int RoleKeysToBackfill { get; init; }
    }
}
