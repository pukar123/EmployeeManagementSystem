using System.Globalization;
using EMS.Application.DTOs.Files;
using EMS.Application.Services.Files;

namespace EMS.API.Services;

/// <summary>
/// Persists organisation logos under wwwroot/attachments/Organization; database stores only the public web-relative path.
/// Logos are the only public uploads: static file middleware serves only <c>/attachments/Organization</c>.
/// </summary>
public sealed class LocalOrganizationLogoStorage
{
    private readonly IWebHostEnvironment _environment;
    private readonly IUploadFileChecker _checker;
    private readonly ILogger<LocalOrganizationLogoStorage> _logger;

    public LocalOrganizationLogoStorage(
        IWebHostEnvironment environment,
        IUploadFileChecker checker,
        ILogger<LocalOrganizationLogoStorage> logger)
    {
        _environment = environment;
        _checker = checker;
        _logger = logger;
    }

    /// <summary>
    /// Checks the file with the shared upload checker (B10: PNG, JPEG or WebP, up to 5 MB; never SVG), then saves it
    /// as <c>logo-{guid}{server extension}</c> and returns a path starting with /attachments/ for static file
    /// middleware. A refused file throws <see cref="UploadRejectedException"/> before anything is written. This does
    /// not delete the current logo: the caller deletes it only after the database points to the new one.
    /// </summary>
    public async Task<string> SaveLogoAsync(int organizationId, IFormFile file, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(file);

        await using var content = file.OpenReadStream();
        var checkedFile = _checker.Check(UploadPurpose.OrganizationLogo, content, file.FileName, file.ContentType, file.Length);

        var webRoot = _environment.WebRootPath
                      ?? throw new InvalidOperationException("WebRootPath is not set. Ensure wwwroot exists.");
        var orgFolder = organizationId.ToString(CultureInfo.InvariantCulture);
        var orgDir = Path.Combine(webRoot, "attachments", "Organization", orgFolder);
        Directory.CreateDirectory(orgDir);

        var fileName = $"logo-{Guid.NewGuid():N}{checkedFile.Extension}";
        var physicalPath = Path.Combine(orgDir, fileName);

        content.Position = 0;
        try
        {
            await using var stream = new FileStream(physicalPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 65536,
                FileOptions.Asynchronous);
            await content.CopyToAsync(stream, cancellationToken);
        }
        catch
        {
            // A part-written logo must not be left behind.
            TryDeletePhysical(physicalPath);
            throw;
        }

        var relative = $"/attachments/Organization/{orgFolder}/{fileName}";
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
        // Only files inside the public logo folder may be deleted. The trailing separator in IsInside stops a
        // sibling folder (for example "Organization-evil") from passing the check.
        var logoRoot = Path.GetFullPath(Path.Combine(webRoot, "attachments", "Organization"));
        if (!PrivateFileStore.IsInside(fullPath, logoRoot))
            return;

        TryDeletePhysical(fullPath);
    }

    private void TryDeletePhysical(string fullPath)
    {
        try
        {
            if (File.Exists(fullPath))
                File.Delete(fullPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not delete logo file {Path}", fullPath);
        }
    }
}
