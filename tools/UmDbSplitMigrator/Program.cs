using Microsoft.Data.SqlClient;
using UmDbSplitMigrator;

MigrationOptions options;
try
{
    options = MigrationOptions.Parse(args);
}
catch (ArgumentException ex)
{
    Console.Error.WriteLine(ex.Message);
    Console.Error.WriteLine();
    MigrationOptions.PrintHelp();
    return 2;
}

Console.WriteLine($"UmDbSplitMigrator mode={options.Mode}");
Console.WriteLine($"Source: {MaskConnectionString(options.SourceConnectionString)}");
Console.WriteLine($"Target: {MaskConnectionString(options.TargetConnectionString)}");

await using var source = new SqlConnection(options.SourceConnectionString);
await using var target = new SqlConnection(options.TargetConnectionString);
await source.OpenAsync();
await target.OpenAsync();

try
{
    switch (options.Mode)
    {
        case MigrationMode.DryRun:
        {
            var migrator = new SplitMigrator(source, target, dryRun: true);
            await migrator.ApplyAsync(CancellationToken.None);
            var preflight = await RunSourcePreflightAsync(source, CancellationToken.None);
            preflight.WriteToConsole();
            return preflight.AllPassed ? 0 : 1;
        }
        case MigrationMode.Apply:
        {
            var migrator = new SplitMigrator(source, target, dryRun: false);
            await migrator.ApplyAsync(CancellationToken.None);
            var report = await new Validator(source, target).RunAsync(CancellationToken.None);
            report.WriteToConsole();
            return report.AllPassed ? 0 : 1;
        }
        case MigrationMode.Validate:
        {
            var report = await new Validator(source, target).RunAsync(CancellationToken.None);
            report.WriteToConsole();
            return report.AllPassed ? 0 : 1;
        }
        case MigrationMode.Rollback:
        {
            if (!options.ConfirmDestructive)
            {
                Console.Error.WriteLine("Rollback requires --confirm.");
                return 2;
            }

            var migrator = new SplitMigrator(source, target, dryRun: false);
            await migrator.RollbackAsync(CancellationToken.None);
            return 0;
        }
        default:
            Console.Error.WriteLine($"Unsupported mode {options.Mode}");
            return 2;
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Migration failed: {ex.Message}");
    Console.Error.WriteLine(ex);
    return 1;
}

static string MaskConnectionString(string connectionString)
{
    try
    {
        var builder = new SqlConnectionStringBuilder(connectionString);
        if (!string.IsNullOrEmpty(builder.Password))
            builder.Password = "***";
        return builder.ConnectionString;
    }
    catch
    {
        return "(unparseable connection string)";
    }
}

static async Task<ValidationReport> RunSourcePreflightAsync(SqlConnection source, CancellationToken cancellationToken)
{
    var report = new ValidationReport();

    var roleIds = new HashSet<string>(StringComparer.Ordinal);
    await using (var cmd = source.CreateCommand())
    {
        cmd.CommandText = "SELECT Id FROM um.Roles";
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            roleIds.Add(reader.GetInt32(0).ToString());
    }

    var unknown = 0L;
    foreach (var table in new[] { "org.PositionRoles", "org.EmployeeRoleAssignments" })
    {
        var parts = table.Split('.');
        if (!await SqlHelpers.TableExistsAsync(source, parts[0], parts[1], cancellationToken))
            continue;
        if (!await SqlHelpers.ColumnExistsAsync(source, parts[0], parts[1], "RoleKey", cancellationToken))
            continue;

        await using var cmd = source.CreateCommand();
        cmd.CommandText = $"SELECT RoleKey FROM {table} WHERE RoleKey LIKE '[0-9]%'";
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            if (!roleIds.Contains(reader.GetString(0)))
                unknown++;
        }
    }

    report.Add(
        "source unknown role IDs",
        unknown == 0,
        unknown == 0
            ? "all numeric RoleKey values map to um.Roles.Id"
            : $"{unknown} numeric RoleKey value(s) do not exist in um.Roles");

    var emailDupes = await SqlHelpers.CountAsync(
        source,
        """
        SELECT COUNT(*) FROM (
            SELECT NormalizedEmail FROM um.Users GROUP BY NormalizedEmail HAVING COUNT(*) > 1
        ) d
        """,
        cancellationToken);
    report.Add("source duplicate emails", emailDupes == 0, $"duplicate_groups={emailDupes}");

    var identityDupes = await SqlHelpers.CountAsync(
        source,
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
        "source duplicate external identity links",
        identityDupes == 0,
        $"duplicate_groups={identityDupes}");

    return report;
}
