namespace EMS.API.IntegrationTests.Storage;

/// <summary>A unique folder under the system temp directory, deleted on dispose. Storage tests never use the repository.</summary>
public sealed class TempFolder : IDisposable
{
    public TempFolder()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"ems-storage-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    /// <summary>Full path of <paramref name="relative"/> (forward slashes) inside this folder.</summary>
    public string Combine(string relative)
        => System.IO.Path.Combine(Path, relative.Replace('/', System.IO.Path.DirectorySeparatorChar));

    /// <summary>Writes <paramref name="content"/> to <paramref name="relative"/>, creating folders, and returns the full path.</summary>
    public string Write(string relative, string content)
    {
        var full = Combine(relative);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(full)!);
        File.WriteAllText(full, content);
        return full;
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(Path))
                Directory.Delete(Path, recursive: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Leaving a temp folder behind must not fail a test.
        }
    }
}
