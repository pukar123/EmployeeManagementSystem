using Microsoft.Data.SqlClient;

namespace UmDbSplitMigrator;

internal sealed class Validator
{
    private readonly SqlConnection _source;
    private readonly SqlConnection _target;

    public Validator(SqlConnection source, SqlConnection target)
    {
        _source = source;
        _target = target;
    }

    public async Task<ValidationReport> RunAsync(CancellationToken cancellationToken)
    {
        var report = new ValidationReport();

        await EnsurePrerequisitesAsync(report, cancellationToken);
        if (!report.AllPassed)
            return report;

        await CheckCountsAsync(report, cancellationToken);
        await CheckIdentityLinksAsync(report, cancellationToken);
        await CheckDuplicateEmailsAsync(report, cancellationToken);
        await CheckDuplicateExternalIdentityLinksAsync(report, cancellationToken);
        await CheckRefreshTokenPolicyAsync(report, cancellationToken);
        await CheckPendingInvitationsAsync(report, cancellationToken);
        await CheckUnknownRoleKeysAsync(report, cancellationToken);
        await CheckMigrationHistoryAsync(report, cancellationToken);

        return report;
    }

    private async Task EnsurePrerequisitesAsync(ValidationReport report, CancellationToken cancellationToken)
    {
        foreach (var table in new[] { "Users", "Roles", "UserRoles", "RefreshTokens" })
        {
            var sourceOk = await SqlHelpers.TableExistsAsync(_source, "um", table, cancellationToken);
            var targetOk = await SqlHelpers.TableExistsAsync(_target, "um", table, cancellationToken);
            report.Add(
                $"schema.um.{table}",
                sourceOk && targetOk,
                sourceOk && targetOk
                    ? "present on source and target"
                    : $"source={sourceOk}, target={targetOk}");
        }

        var targetInvites = await SqlHelpers.TableExistsAsync(_target, "um", "AccountInvitations", cancellationToken);
        report.Add(
            "schema.um.AccountInvitations",
            targetInvites,
            targetInvites
                ? "present on target"
                : "missing on target — apply UM EF migrations first");
    }

    private async Task CheckCountsAsync(ValidationReport report, CancellationToken cancellationToken)
    {
        var sourceUsers = await SqlHelpers.CountAsync(_source, "SELECT COUNT(*) FROM um.Users", cancellationToken);
        var targetUsers = await SqlHelpers.CountAsync(_target, "SELECT COUNT(*) FROM um.Users", cancellationToken);
        report.Add("user counts", sourceUsers == targetUsers, $"source={sourceUsers}, target={targetUsers}");

        var sourceRoles = await SqlHelpers.CountAsync(_source, "SELECT COUNT(*) FROM um.Roles", cancellationToken);
        var targetRoles = await SqlHelpers.CountAsync(_target, "SELECT COUNT(*) FROM um.Roles", cancellationToken);
        report.Add("role counts", sourceRoles == targetRoles, $"source={sourceRoles}, target={targetRoles}");

        var sourceUserRoles = await SqlHelpers.CountAsync(_source, "SELECT COUNT(*) FROM um.UserRoles", cancellationToken);
        var targetUserRoles = await SqlHelpers.CountAsync(_target, "SELECT COUNT(*) FROM um.UserRoles", cancellationToken);
        report.Add(
            "user-role assignments",
            sourceUserRoles == targetUserRoles,
            $"source={sourceUserRoles}, target={targetUserRoles}");

        var sourceUserIds = await ReadIdsAsync(_source, "SELECT Id FROM um.Users", cancellationToken);
        var targetUserIds = await ReadIdsAsync(_target, "SELECT Id FROM um.Users", cancellationToken);
        var missingUserIds = sourceUserIds.Except(targetUserIds).OrderBy(x => x).ToList();
        report.Add(
            "user id preservation",
            missingUserIds.Count == 0,
            missingUserIds.Count == 0
                ? "all source user ids present on target"
                : $"missing target ids: {string.Join(", ", missingUserIds.Take(20))}" +
                  (missingUserIds.Count > 20 ? "..." : string.Empty));

        var sourceRoleIds = await ReadIdsAsync(_source, "SELECT Id FROM um.Roles", cancellationToken);
        var targetRoleIds = await ReadIdsAsync(_target, "SELECT Id FROM um.Roles", cancellationToken);
        var missingRoleIds = sourceRoleIds.Except(targetRoleIds).OrderBy(x => x).ToList();
        report.Add(
            "role id preservation",
            missingRoleIds.Count == 0,
            missingRoleIds.Count == 0
                ? "all source role ids present on target"
                : $"missing target ids: {string.Join(", ", missingRoleIds.Take(20))}");
    }

    private async Task CheckIdentityLinksAsync(ValidationReport report, CancellationToken cancellationToken)
    {
        var activeLinks = await SqlHelpers.CountAsync(
            _source,
            """
            SELECT COUNT(*)
            FROM org.Employees
            WHERE IsArchived = 0
              AND ExternalIdentityKey IS NOT NULL
              AND LTRIM(RTRIM(ExternalIdentityKey)) <> ''
            """,
            cancellationToken);

        var brokenLinks = await CountBrokenIdentityLinksAsync(cancellationToken);
        report.Add(
            "active employee identity links",
            brokenLinks == 0,
            brokenLinks == 0
                ? $"{activeLinks} active links resolve to target users"
                : $"{brokenLinks} of {activeLinks} active links do not resolve to target um.Users");
    }

    private async Task<long> CountBrokenIdentityLinksAsync(CancellationToken cancellationToken)
    {
        var targetUserIds = (await ReadIdsAsync(_target, "SELECT Id FROM um.Users", cancellationToken))
            .Select(id => id.ToString())
            .ToHashSet(StringComparer.Ordinal);

        await using var cmd = _source.CreateCommand();
        cmd.CommandText =
            """
            SELECT ExternalIdentityKey
            FROM org.Employees
            WHERE IsArchived = 0
              AND ExternalIdentityKey IS NOT NULL
              AND LTRIM(RTRIM(ExternalIdentityKey)) <> ''
            """;

        var broken = 0L;
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var key = reader.GetString(0).Trim();
            if (!targetUserIds.Contains(key))
                broken++;
        }

        return broken;
    }

    private async Task CheckDuplicateEmailsAsync(ValidationReport report, CancellationToken cancellationToken)
    {
        var sourceDupes = await SqlHelpers.CountAsync(
            _source,
            """
            SELECT COUNT(*) FROM (
                SELECT NormalizedEmail
                FROM um.Users
                GROUP BY NormalizedEmail
                HAVING COUNT(*) > 1
            ) d
            """,
            cancellationToken);
        var targetDupes = await SqlHelpers.CountAsync(
            _target,
            """
            SELECT COUNT(*) FROM (
                SELECT NormalizedEmail
                FROM um.Users
                GROUP BY NormalizedEmail
                HAVING COUNT(*) > 1
            ) d
            """,
            cancellationToken);

        report.Add(
            "duplicate emails",
            sourceDupes == 0 && targetDupes == 0,
            $"source_duplicate_groups={sourceDupes}, target_duplicate_groups={targetDupes}");
    }

    private async Task CheckDuplicateExternalIdentityLinksAsync(ValidationReport report, CancellationToken cancellationToken)
    {
        var dupes = await SqlHelpers.CountAsync(
            _source,
            """
            SELECT COUNT(*) FROM (
                SELECT ExternalIdentityKey
                FROM org.Employees
                WHERE IsArchived = 0
                  AND ExternalIdentityKey IS NOT NULL
                  AND LTRIM(RTRIM(ExternalIdentityKey)) <> ''
                GROUP BY ExternalIdentityKey
                HAVING COUNT(*) > 1
            ) d
            """,
            cancellationToken);

        report.Add(
            "duplicate external identity links",
            dupes == 0,
            dupes == 0
                ? "no active employees share ExternalIdentityKey"
                : $"{dupes} ExternalIdentityKey values are shared by multiple active employees");
    }

    private async Task CheckRefreshTokenPolicyAsync(ValidationReport report, CancellationToken cancellationToken)
    {
        var sourceActive = await SqlHelpers.CountAsync(
            _source,
            """
            SELECT COUNT(*)
            FROM um.RefreshTokens
            WHERE RevokedAtUtc IS NULL
              AND ExpiresAtUtc > SYSUTCDATETIME()
            """,
            cancellationToken);
        var targetActive = await SqlHelpers.CountAsync(
            _target,
            """
            SELECT COUNT(*)
            FROM um.RefreshTokens
            WHERE RevokedAtUtc IS NULL
              AND ExpiresAtUtc > SYSUTCDATETIME()
            """,
            cancellationToken);

        var orphanTokens = await SqlHelpers.CountAsync(
            _target,
            """
            SELECT COUNT(*)
            FROM um.RefreshTokens rt
            WHERE NOT EXISTS (SELECT 1 FROM um.Users u WHERE u.Id = rt.UserId)
            """,
            cancellationToken);

        var sourceTotal = await SqlHelpers.CountAsync(_source, "SELECT COUNT(*) FROM um.RefreshTokens", cancellationToken);
        var targetTotal = await SqlHelpers.CountAsync(_target, "SELECT COUNT(*) FROM um.RefreshTokens", cancellationToken);

        var passed = sourceActive == targetActive && sourceTotal == targetTotal && orphanTokens == 0;
        report.Add(
            "refresh-token policy",
            passed,
            $"active source={sourceActive}, active target={targetActive}, total source={sourceTotal}, total target={targetTotal}, orphans={orphanTokens}");
    }

    private async Task CheckPendingInvitationsAsync(ValidationReport report, CancellationToken cancellationToken)
    {
        var sourceHasInvites = await SqlHelpers.TableExistsAsync(_source, "emp", "EmployeeInvitations", cancellationToken);
        if (!sourceHasInvites)
        {
            report.Add(
                "pending invitations",
                true,
                "emp.EmployeeInvitations not present on source (already dropped or never created); skipped");
            return;
        }

        var sourcePending = await SqlHelpers.CountAsync(
            _source,
            """
            SELECT COUNT(*)
            FROM emp.EmployeeInvitations
            WHERE UsedAtUtc IS NULL
              AND RevokedAtUtc IS NULL
              AND ExpiresAtUtc > SYSUTCDATETIME()
            """,
            cancellationToken);

        var targetPending = await SqlHelpers.CountAsync(
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

        report.Add(
            "pending invitations",
            sourcePending == targetPending,
            $"source_pending={sourcePending}, target_pending_employee_refs={targetPending}");
    }

    private async Task CheckUnknownRoleKeysAsync(ValidationReport report, CancellationToken cancellationToken)
    {
        var roleKeys = await ReadRoleKeysAsync(cancellationToken);
        var knownKeys = roleKeys.NormalizedNames;
        var knownIds = roleKeys.IdsAsStrings;

        var unknownPosition = 0L;
        var unknownAssignment = 0L;
        var legacyNumericRemaining = 0L;

        if (await SqlHelpers.TableExistsAsync(_source, "org", "PositionRoles", cancellationToken))
        {
            await using var cmd = _source.CreateCommand();
            cmd.CommandText = "SELECT RoleKey FROM org.PositionRoles";
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var key = reader.GetString(0);
                if (IsNumericKey(key))
                {
                    legacyNumericRemaining++;
                    if (!knownIds.Contains(key))
                        unknownPosition++;
                }
                else if (!knownKeys.Contains(key))
                {
                    unknownPosition++;
                }
            }
        }

        if (await SqlHelpers.TableExistsAsync(_source, "org", "EmployeeRoleAssignments", cancellationToken))
        {
            await using var cmd = _source.CreateCommand();
            cmd.CommandText = "SELECT RoleKey FROM org.EmployeeRoleAssignments";
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var key = reader.GetString(0);
                if (IsNumericKey(key))
                {
                    legacyNumericRemaining++;
                    if (!knownIds.Contains(key))
                        unknownAssignment++;
                }
                else if (!knownKeys.Contains(key))
                {
                    unknownAssignment++;
                }
            }
        }

        var passed = unknownPosition == 0 && unknownAssignment == 0 && legacyNumericRemaining == 0;
        report.Add(
            "unknown role IDs / keys",
            passed,
            passed
                ? "all PositionRole and EmployeeRoleAssignment keys resolve to UM NormalizedName"
                : $"unknown_position={unknownPosition}, unknown_assignment={unknownAssignment}, legacy_numeric_remaining={legacyNumericRemaining}");
    }

    private async Task CheckMigrationHistoryAsync(ValidationReport report, CancellationToken cancellationToken)
    {
        var sourceHistory = await ReadMigrationIdsAsync(_source, cancellationToken);
        var targetHistory = await ReadMigrationIdsAsync(_target, cancellationToken);
        var expected = SplitMigrator.UmMigrationIds;
        var missingOnTarget = expected.Where(id => !targetHistory.Contains(id)).ToList();
        var presentOnSource = expected.Count(id => sourceHistory.Contains(id));

        report.Add(
            "UM migration history",
            missingOnTarget.Count == 0,
            missingOnTarget.Count == 0
                ? $"target has all {expected.Length} UM migrations (source had {presentOnSource})"
                : $"missing on target: {string.Join(", ", missingOnTarget)}");
    }

    private async Task<(HashSet<string> NormalizedNames, HashSet<string> IdsAsStrings)> ReadRoleKeysAsync(
        CancellationToken cancellationToken)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        var ids = new HashSet<string>(StringComparer.Ordinal);
        await using var cmd = _target.CreateCommand();
        cmd.CommandText = "SELECT Id, NormalizedName FROM um.Roles";
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            ids.Add(reader.GetInt32(0).ToString());
            names.Add(reader.GetString(1));
        }

        // Fall back to source roles if target is empty (pre-apply validation).
        if (names.Count == 0)
        {
            await using var sourceCmd = _source.CreateCommand();
            sourceCmd.CommandText = "SELECT Id, NormalizedName FROM um.Roles";
            await using var sourceReader = await sourceCmd.ExecuteReaderAsync(cancellationToken);
            while (await sourceReader.ReadAsync(cancellationToken))
            {
                ids.Add(sourceReader.GetInt32(0).ToString());
                names.Add(sourceReader.GetString(1));
            }
        }

        return (names, ids);
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

    private static bool IsNumericKey(string key) =>
        key.Length > 0 && key.All(char.IsDigit);
}
