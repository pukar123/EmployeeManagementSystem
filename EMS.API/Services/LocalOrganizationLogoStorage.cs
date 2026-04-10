namespace EMS.API.Services;

/// <summary>
/// Persists organization logos under wwwroot/attachments/Organization; database stores only the web-relative path.
/// </summary>
public sealed class LocalOrganizationLogoStorage
{
    private const long MaxBytes = 5 * 1024 * 1024;
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".webp", ".gif", ".svg",
    };

    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<LocalOrganizationLogoStorage> _logger;

    public LocalOrganizationLogoStorage(IWebHostEnvironment environment, ILogger<LocalOrganizationLogoStorage> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    /// <summary>
    /// Saves the file and returns a path starting with /attachments/ suitable for static file middleware.
    /// </summary>
    public async Task<string> SaveLogoAsync(int organizationId, IFormFile file, CancellationToken cancellationToken = default)
    {
        if (file.Length == 0)
            throw new ArgumentException("Empty file.", nameof(file));
        if (file.Length > MaxBytes)
            throw new InvalidOperationException($"File exceeds maximum size of {MaxBytes / 1024 / 1024} MB.");

        var ext = Path.GetExtension(file.FileName);
        if (string.IsNullOrEmpty(ext) || !AllowedExtensions.Contains(ext))
            throw new InvalidOperationException("Allowed types: PNG, JPG, JPEG, WEBP, GIF, SVG.");

        var webRoot = _environment.WebRootPath
                      ?? throw new InvalidOperationException("WebRootPath is not set. Ensure wwwroot exists.");
        var orgDir = Path.Combine(webRoot, "attachments", "Organization", organizationId.ToString());
        Directory.CreateDirectory(orgDir);

        var fileName = $"logo-{Guid.NewGuid():N}{ext}";
        var physicalPath = Path.Combine(orgDir, fileName);

        await using (var stream = new FileStream(physicalPath, FileMode.Create, FileAccess.Write, FileShare.None, 65536,
                       FileOptions.Asynchronous))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        var relative = $"/attachments/Organization/{organizationId}/{fileName}".Replace('\\', '/');
        _logger.LogInformation("Saved organization logo for org {OrgId} at {Path}", organizationId, relative);
        return relative;
    }

    public void TryDeleteFile(string? webRelativePath)
    {
        if (string.IsNullOrWhiteSpace(webRelativePath) ||
            (!webRelativePath.StartsWith("/attachments/", StringComparison.Ordinal) &&
             !webRelativePath.StartsWith("/uploads/", StringComparison.Ordinal)))
            return;

        var webRoot = _environment.WebRootPath;
        if (string.IsNullOrEmpty(webRoot))
            return;

        var trimmed = webRelativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(webRoot, trimmed));
        var rootFull = Path.GetFullPath(webRoot);
        if (!fullPath.StartsWith(rootFull, StringComparison.OrdinalIgnoreCase))
            return;

        try
        {
            if (File.Exists(fullPath))
                File.Delete(fullPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not delete old logo file {Path}", fullPath);
        }
    }
}
