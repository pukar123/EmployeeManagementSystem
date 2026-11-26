namespace EMS.API.Services;

/// <summary>
/// The single place that reads, writes and deletes private files (employee documents and leave attachments)
/// under <c>Storage:PrivateRoot</c>. Every stored key goes through <see cref="TryResolve"/>, which only accepts
/// keys that stay inside the root and start with an allowed top-level folder.
/// Organisation logos are public and do not use this store.
/// </summary>
public sealed class PrivateFileStore
{
    public const string EmployeeFolder = "Employee";
    public const string LeaveFolder = "Leave";

    /// <summary>Prefix of paths saved before B08, when these files lived under <c>wwwroot/attachments</c>.</summary>
    public const string LegacyPublicPrefix = "/attachments/";

    private static readonly string[] AllowedTopFolders = [EmployeeFolder, LeaveFolder];

    private readonly ILogger<PrivateFileStore> _logger;

    public PrivateFileStore(string rootPath, ILogger<PrivateFileStore> logger)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
            throw new ArgumentException("The private storage root is required.", nameof(rootPath));

        RootPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(rootPath));
        _logger = logger;
    }

    /// <summary>Canonical full path of the private root, without a trailing separator.</summary>
    public string RootPath { get; }

    /// <summary>Ordinal-ignore-case on Windows (case-insensitive file system), ordinal elsewhere.</summary>
    public static StringComparison PathComparison
        => OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    /// <summary>
    /// Resolves <c>Storage:PrivateRoot</c>: a relative path resolves against <paramref name="contentRootPath"/>,
    /// an absolute path is used as given. Throws when the result is the web root or inside it, because static
    /// file middleware could then serve private files.
    /// </summary>
    public static string ResolveRoot(string? configuredRoot, string contentRootPath, string webRootPath)
    {
        var configured = string.IsNullOrWhiteSpace(configuredRoot) ? "App_Data/private" : configuredRoot.Trim();
        var full = Path.TrimEndingDirectorySeparator(Path.IsPathRooted(configured)
            ? Path.GetFullPath(configured)
            : Path.GetFullPath(Path.Combine(contentRootPath, configured)));
        var webRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(webRootPath));

        if (string.Equals(full, webRoot, PathComparison) || IsInside(full, webRoot))
        {
            throw new InvalidOperationException(
                $"Storage:PrivateRoot ('{full}') must not be the web root or a folder inside it ('{webRoot}'). " +
                "Private files would be publicly reachable. Choose a folder outside wwwroot, for example App_Data/private.");
        }

        return full;
    }

    /// <summary>
    /// True when <paramref name="fullPath"/> is strictly inside <paramref name="rootPath"/>. Compares against the
    /// root plus a trailing separator, so a sibling such as <c>{root}-evil</c> is not inside.
    /// Both paths must already be canonical (<see cref="Path.GetFullPath(string)"/>).
    /// </summary>
    public static bool IsInside(string fullPath, string rootPath)
    {
        var root = Path.TrimEndingDirectorySeparator(rootPath) + Path.DirectorySeparatorChar;
        return fullPath.StartsWith(root, PathComparison);
    }

    /// <summary>
    /// Normalises a stored path to a storage key (<c>Leave/12/a.txt</c>) and checks its shape only (no file system
    /// access): rejects empty input, <c>\0</c>, <c>:</c>, rooted keys, <c>.</c>, <c>..</c> and empty segments, and any
    /// first folder other than <c>Employee</c> or <c>Leave</c>. Legacy <c>/attachments/...</c> paths are accepted.
    /// </summary>
    public static bool TryNormaliseKey(string? storedPath, out string key)
    {
        key = string.Empty;
        if (string.IsNullOrWhiteSpace(storedPath) || storedPath.Contains('\0') || storedPath.Contains(':'))
            return false;

        var candidate = storedPath;
        if (candidate.StartsWith(LegacyPublicPrefix, StringComparison.Ordinal))
            candidate = candidate[LegacyPublicPrefix.Length..];
        else if (candidate.StartsWith('/'))
            candidate = candidate[1..];

        candidate = candidate.Replace('\\', '/');

        if (candidate.Length == 0 || Path.IsPathRooted(candidate) || candidate.StartsWith('/'))
            return false;

        var segments = candidate.Split('/');
        if (segments.Any(s => s.Length == 0 || s == "." || s == ".."))
            return false;

        if (!AllowedTopFolders.Contains(segments[0], StringComparer.Ordinal))
            return false;

        key = candidate;
        return true;
    }

    /// <summary>
    /// Resolves a stored path to a full path inside <see cref="RootPath"/>. Returns false for any key that is
    /// unsafe or outside the root; callers treat that as "not found".
    /// </summary>
    public bool TryResolve(string? storedPath, out string fullPath)
    {
        fullPath = string.Empty;
        if (!TryNormaliseKey(storedPath, out var key))
            return false;

        string full;
        try
        {
            full = Path.GetFullPath(Path.Combine(RootPath, key.Replace('/', Path.DirectorySeparatorChar)));
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return false;
        }

        if (!IsInside(full, RootPath))
            return false;

        fullPath = full;
        return true;
    }

    /// <summary>True when the stored path is safe and its file exists.</summary>
    public bool Exists(string? storedPath)
        => TryResolve(storedPath, out var full) && File.Exists(full);

    /// <summary>
    /// Saves <paramref name="content"/> as <c>{folderKey}/{guid}{serverExtension}</c> and returns that storage key.
    /// <paramref name="folderKey"/> is, for example, <c>Employee/5/Documents</c> or <c>Leave/12</c>.
    /// <paramref name="serverExtension"/> comes from the upload checker (B10), never from the uploaded file name:
    /// only a lower-case dot followed by 3 or 4 letters is accepted, otherwise nothing is written.
    /// </summary>
    public async Task<string> SaveAsync(
        string folderKey,
        Stream content,
        string serverExtension,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        if (!IsServerExtension(serverExtension))
            throw new InvalidOperationException("The file could not be stored. The file type is not supported.");

        var key = $"{folderKey.Trim('/')}/{Guid.NewGuid():N}{serverExtension}";
        if (!TryResolve(key, out var fullPath))
            throw new InvalidOperationException("The file could not be stored. Check the file name and try again.");

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        if (content.CanSeek)
            content.Position = 0;

        try
        {
            await using var stream = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 65536,
                FileOptions.Asynchronous);
            await content.CopyToAsync(stream, cancellationToken);
        }
        catch
        {
            // A part-written file must not be left behind.
            TryDelete(key);
            throw;
        }

        return key;
    }

    /// <summary>True for a lower-case dot followed by 3 or 4 ASCII letters (for example <c>.pdf</c>, <c>.docx</c>).</summary>
    public static bool IsServerExtension(string? extension)
        => extension is { Length: 4 or 5 } && extension[0] == '.' && extension[1..].All(char.IsAsciiLetterLower);

    /// <summary>Deletes the file for a stored path if it is safe and exists. Never throws for IO errors.</summary>
    public void TryDelete(string? storedPath)
    {
        if (!TryResolve(storedPath, out var fullPath))
            return;

        try
        {
            if (File.Exists(fullPath))
                File.Delete(fullPath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(ex, "Could not delete private file {StoragePath}", storedPath);
        }
    }
}
