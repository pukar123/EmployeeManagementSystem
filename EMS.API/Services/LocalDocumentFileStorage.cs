using EMS.Domain.Enums;

namespace EMS.API.Services;

/// <summary>
/// Stores employee documents under wwwroot/attachments/Employee; database holds only web-relative paths.
/// </summary>
public sealed class LocalDocumentFileStorage
{
    private readonly IWebHostEnvironment _env;
    private const long MaxBytes = 15 * 1024 * 1024;

    public LocalDocumentFileStorage(IWebHostEnvironment env)
    {
        _env = env;
    }

    public static bool TryResolveFileKind(string contentType, string fileName, out DocumentFileKind kind)
    {
        kind = DocumentFileKind.None;
        var ct = contentType ?? string.Empty;
        var ext = Path.GetExtension(fileName).ToLowerInvariant();

        if (ct.Contains("pdf", StringComparison.OrdinalIgnoreCase) || ext == ".pdf")
        {
            kind = DocumentFileKind.Pdf;
            return true;
        }

        if (ct.Contains("wordprocessingml", StringComparison.OrdinalIgnoreCase) ||
            ct.Contains("msword", StringComparison.OrdinalIgnoreCase) ||
            ext is ".doc" or ".docx")
        {
            kind = DocumentFileKind.Word;
            return true;
        }

        if (ct.StartsWith("image/", StringComparison.OrdinalIgnoreCase) ||
            ext is ".jpg" or ".jpeg" or ".png" or ".gif" or ".webp" or ".bmp")
        {
            kind = DocumentFileKind.Image;
            return true;
        }

        return false;
    }

    public static void ValidateContentOrThrow(IFormFile file)
    {
        if (file.Length == 0)
            throw new InvalidOperationException("File is empty.");
        if (file.Length > MaxBytes)
            throw new InvalidOperationException($"File exceeds maximum size ({MaxBytes / (1024 * 1024)} MB).");

        if (!TryResolveFileKind(file.ContentType, file.FileName, out _))
            throw new InvalidOperationException("Only PDF, Word (.doc/.docx), or image files are allowed.");
    }

    /// <summary>
    /// Saves under attachments/Employee/{employeeId|unassigned}/Documents and returns path starting with /attachments/...
    /// </summary>
    public async Task<(string RelativePath, string ContentType, DocumentFileKind Kind)> SaveAsync(
        int? employeeId,
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        ValidateContentOrThrow(file);
        if (!TryResolveFileKind(file.ContentType, file.FileName, out var kind))
            throw new InvalidOperationException("Could not determine file kind.");

        var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        var folder = employeeId is int id ? id.ToString() : "unassigned";
        var dir = Path.Combine(webRoot, "attachments", "Employee", folder, "Documents");
        Directory.CreateDirectory(dir);

        var ext = Path.GetExtension(file.FileName);
        var safeName = $"{Guid.NewGuid():N}{ext}";
        var physical = Path.Combine(dir, safeName);

        await using (var stream = File.Create(physical))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        var relative = $"/attachments/Employee/{folder}/Documents/{safeName}".Replace('\\', '/');
        return (relative, file.ContentType, kind);
    }

    public void TryDelete(string? webRelativePath)
    {
        if (string.IsNullOrWhiteSpace(webRelativePath) ||
            (!webRelativePath.StartsWith("/attachments/", StringComparison.Ordinal) &&
             !webRelativePath.StartsWith("/uploads/", StringComparison.Ordinal)))
            return;

        var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        var relative = webRelativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var full = Path.GetFullPath(Path.Combine(webRoot, relative));
        var root = Path.GetFullPath(webRoot);
        if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            return;
        if (File.Exists(full))
            File.Delete(full);
    }
}
