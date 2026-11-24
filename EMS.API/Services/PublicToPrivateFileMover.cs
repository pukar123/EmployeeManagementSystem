using EMS.Domain.Database;
using Microsoft.EntityFrameworkCore;

namespace EMS.API.Services;

/// <summary>
/// One-off, repeatable move of employee document and leave attachment files from the old public folder
/// (<c>wwwroot/attachments/Employee</c> and <c>wwwroot/attachments/Leave</c>) into the private root, updating each
/// row's stored path from <c>/attachments/...</c> to the storage key. It never overwrites a file, leaves a row
/// unchanged when its file is missing, and a second run moves and updates nothing. Logos are never touched.
/// Run at start-up by <see cref="EMS.API.Bootstrap.PrivateFileMoveHostedService"/> when
/// <c>Storage:MovePublicFilesOnStartup</c> is true.
/// </summary>
public sealed class PublicToPrivateFileMover
{
    private static readonly string[] LegacyPrefixes =
    [
        PrivateFileStore.LegacyPublicPrefix + PrivateFileStore.EmployeeFolder + "/",
        PrivateFileStore.LegacyPublicPrefix + PrivateFileStore.LeaveFolder + "/",
    ];

    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _environment;
    private readonly PrivateFileStore _store;
    private readonly ILogger<PublicToPrivateFileMover> _logger;

    public PublicToPrivateFileMover(
        AppDbContext db,
        IWebHostEnvironment environment,
        PrivateFileStore store,
        ILogger<PublicToPrivateFileMover> logger)
    {
        _db = db;
        _environment = environment;
        _store = store;
        _logger = logger;
    }

    public async Task<FileMoveResult> MoveAsync(CancellationToken cancellationToken = default)
    {
        var result = new FileMoveResult();
        var webRoot = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
        var publicRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(Path.Combine(webRoot, "attachments")));
        var handledSources = new HashSet<string>(
            OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);

        var documents = await _db.Documents.IgnoreQueryFilters().ToListAsync(cancellationToken);
        foreach (var document in documents)
        {
            var updated = MoveRow("Document", document.Id, document.StoredRelativePath, publicRoot, handledSources, result);
            if (updated is not null)
                document.StoredRelativePath = updated;
        }

        var attachments = await _db.LeaveRequestAttachments.IgnoreQueryFilters().ToListAsync(cancellationToken);
        foreach (var attachment in attachments)
        {
            var updated = MoveRow("LeaveRequestAttachment", attachment.Id, attachment.StoragePath, publicRoot, handledSources, result);
            if (updated is not null)
                attachment.StoragePath = updated;
        }

        // Saved after the files have moved. If the process stops in between, the next run finds the targets
        // already in place and only updates the rows.
        if (result.PathsUpdated > 0)
            await _db.SaveChangesAsync(cancellationToken);

        SweepOrphans(publicRoot, PrivateFileStore.EmployeeFolder, handledSources, result);
        SweepOrphans(publicRoot, PrivateFileStore.LeaveFolder, handledSources, result);

        return result;
    }

    /// <summary>Handles one row. Returns the new stored path when the row should change, otherwise null.</summary>
    private string? MoveRow(
        string kind,
        int id,
        string storedPath,
        string publicRoot,
        HashSet<string> handledSources,
        FileMoveResult result)
    {
        var isLegacy = LegacyPrefixes.Any(p => storedPath.StartsWith(p, StringComparison.Ordinal));
        if (!isLegacy)
        {
            if (!storedPath.StartsWith('/') && _store.TryResolve(storedPath, out var privatePath))
            {
                result.AlreadyPrivate++;
                if (!File.Exists(privatePath))
                {
                    result.Missing++;
                    _logger.LogWarning("{Kind} {Id}: private file {StoragePath} is missing.", kind, id, storedPath);
                }
            }
            else
            {
                result.Skipped++;
                _logger.LogWarning("{Kind} {Id}: stored path {StoragePath} is not a known file location; left unchanged.", kind, id, storedPath);
            }

            return null;
        }

        var key = storedPath[PrivateFileStore.LegacyPublicPrefix.Length..];
        if (!_store.TryResolve(key, out var target))
        {
            result.Skipped++;
            _logger.LogWarning("{Kind} {Id}: stored path {StoragePath} is unsafe; left unchanged.", kind, id, storedPath);
            return null;
        }

        var source = Path.GetFullPath(Path.Combine(publicRoot, key.Replace('/', Path.DirectorySeparatorChar)));
        if (!PrivateFileStore.IsInside(source, publicRoot))
        {
            result.Skipped++;
            _logger.LogWarning("{Kind} {Id}: stored path {StoragePath} is outside the public attachments folder; left unchanged.", kind, id, storedPath);
            return null;
        }

        handledSources.Add(source);

        try
        {
            if (File.Exists(target))
            {
                if (File.Exists(source))
                {
                    if (new FileInfo(source).Length == new FileInfo(target).Length)
                    {
                        File.Delete(source);
                    }
                    else
                    {
                        result.Conflicts++;
                        _logger.LogWarning(
                            "{Kind} {Id}: a different file already exists at the private location for {StoragePath}; both kept.",
                            kind, id, storedPath);
                    }
                }

                result.PathsUpdated++;
                return key;
            }

            if (File.Exists(source))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                File.Move(source, target, overwrite: false);
                result.Moved++;
                result.PathsUpdated++;
                return key;
            }

            result.Missing++;
            _logger.LogWarning("{Kind} {Id}: file for {StoragePath} was not found; row left unchanged.", kind, id, storedPath);
            return null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            result.Skipped++;
            _logger.LogWarning(ex, "{Kind} {Id}: could not move the file for {StoragePath}; row left unchanged.", kind, id, storedPath);
            return null;
        }
    }

    /// <summary>Moves files no row refers to, so no private file is left in the public folder.</summary>
    private void SweepOrphans(string publicRoot, string folder, HashSet<string> handledSources, FileMoveResult result)
    {
        var folderPath = Path.Combine(publicRoot, folder);
        if (!Directory.Exists(folderPath))
            return;

        foreach (var source in Directory.EnumerateFiles(folderPath, "*", SearchOption.AllDirectories).ToList())
        {
            var fullSource = Path.GetFullPath(source);
            if (handledSources.Contains(fullSource))
                continue;

            var key = Path.GetRelativePath(publicRoot, fullSource).Replace('\\', '/');
            if (!_store.TryResolve(key, out var target))
            {
                result.Skipped++;
                _logger.LogWarning("Unreferenced public file {Key} has an unsafe name; left in place.", key);
                continue;
            }

            try
            {
                if (File.Exists(target))
                {
                    result.Conflicts++;
                    _logger.LogWarning("Unreferenced public file {Key} already exists at the private location; both kept.", key);
                    continue;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                File.Move(fullSource, target, overwrite: false);
                result.OrphansMoved++;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                result.Skipped++;
                _logger.LogWarning(ex, "Could not move unreferenced public file {Key}.", key);
            }
        }
    }
}
