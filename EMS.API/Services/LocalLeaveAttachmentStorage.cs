namespace EMS.API.Services;

public sealed class LocalLeaveAttachmentStorage
{
    private readonly IWebHostEnvironment _env;
    private const long MaxBytes = 15 * 1024 * 1024;

    public LocalLeaveAttachmentStorage(IWebHostEnvironment env)
    {
        _env = env;
    }

    public static void ValidateContentOrThrow(IFormFile file)
    {
        if (file.Length == 0)
            throw new InvalidOperationException("File is empty.");
        if (file.Length > MaxBytes)
            throw new InvalidOperationException($"File exceeds maximum size ({MaxBytes / (1024 * 1024)} MB).");
    }

    public async Task<(string RelativePath, string ContentType, string FileName, long Size)> SaveAsync(
        int leaveRequestId,
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        ValidateContentOrThrow(file);

        var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        var dir = Path.Combine(webRoot, "attachments", "Leave", leaveRequestId.ToString());
        Directory.CreateDirectory(dir);

        var ext = Path.GetExtension(file.FileName);
        var safeName = $"{Guid.NewGuid():N}{ext}";
        var physical = Path.Combine(dir, safeName);

        await using (var stream = File.Create(physical))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        var relative = $"/attachments/Leave/{leaveRequestId}/{safeName}".Replace('\\', '/');
        return (relative, file.ContentType, file.FileName, file.Length);
    }

    public void TryDelete(string? webRelativePath)
    {
        if (string.IsNullOrWhiteSpace(webRelativePath) ||
            !webRelativePath.StartsWith("/attachments/", StringComparison.Ordinal))
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
