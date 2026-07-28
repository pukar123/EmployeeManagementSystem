namespace UmDbSplitMigrator;

internal enum MigrationMode
{
    DryRun,
    Apply,
    Validate,
    Rollback,
}

internal sealed class MigrationOptions
{
    public required MigrationMode Mode { get; init; }

    public required string SourceConnectionString { get; init; }

    public required string TargetConnectionString { get; init; }

    /// <summary>Required for rollback to avoid accidental data loss.</summary>
    public bool ConfirmDestructive { get; init; }

    public static MigrationOptions Parse(string[] args)
    {
        string? mode = null;
        string? source = Environment.GetEnvironmentVariable("EMS_SOURCE_CONNECTION")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        string? target = Environment.GetEnvironmentVariable("UM_TARGET_CONNECTION")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__UserManagementDb");
        var confirm = false;

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg is "-h" or "--help")
            {
                PrintHelp();
                Environment.Exit(0);
            }

            if (arg is "--confirm")
            {
                confirm = true;
                continue;
            }

            if (i + 1 >= args.Length)
                throw new ArgumentException($"Missing value for '{arg}'.");

            var value = args[++i];
            switch (arg)
            {
                case "--mode":
                    mode = value;
                    break;
                case "--source":
                    source = value;
                    break;
                case "--target":
                    target = value;
                    break;
                default:
                    throw new ArgumentException($"Unknown argument '{arg}'.");
            }
        }

        if (string.IsNullOrWhiteSpace(mode))
            throw new ArgumentException("Required: --mode dry-run|apply|validate|rollback");
        if (string.IsNullOrWhiteSpace(source))
            throw new ArgumentException("Required: --source <EMS connection string> (or EMS_SOURCE_CONNECTION).");
        if (string.IsNullOrWhiteSpace(target))
            throw new ArgumentException("Required: --target <UserManagementDb connection string> (or UM_TARGET_CONNECTION).");

        if (!Enum.TryParse<MigrationMode>(mode.Replace("-", string.Empty), ignoreCase: true, out var parsedMode))
            throw new ArgumentException($"Invalid mode '{mode}'. Use dry-run, apply, validate, or rollback.");

        return new MigrationOptions
        {
            Mode = parsedMode,
            SourceConnectionString = source,
            TargetConnectionString = target,
            ConfirmDestructive = confirm,
        };
    }

    public static void PrintHelp()
    {
        Console.WriteLine(
            """
            UmDbSplitMigrator — copy User Management data from EMS DB into UserManagementDb.

            Usage:
              dotnet run --project tools/UmDbSplitMigrator -- --mode <mode> --source <ems-cs> --target <um-cs> [--confirm]

            Modes:
              dry-run    Report planned work and pre-checks; no writes.
              apply      Idempotently copy UM tables, invitations, migration history; backfill role keys.
              validate   Run cutover validation checks; exit 1 on failure.
              rollback   Remove migrated rows from UserManagementDb and restore RoleKey snapshots.
                         Does not delete source EMS um.* tables. Requires --confirm.

            Connection strings may also be supplied via:
              EMS_SOURCE_CONNECTION / ConnectionStrings__DefaultConnection
              UM_TARGET_CONNECTION / ConnectionStrings__UserManagementDb

            Preconditions for apply:
              1. Back up both databases.
              2. Create UserManagementDb and apply UM EF migrations (empty schema).
              3. Source EMS database still contains um.Users / um.Roles / etc.
            """);
    }
}
