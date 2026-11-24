using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;

namespace EMS.API.IntegrationTests.Storage;

/// <summary>Minimal <see cref="IWebHostEnvironment"/> for storage unit tests (no host).</summary>
public sealed class TestWebHostEnvironment : IWebHostEnvironment
{
    public TestWebHostEnvironment(string webRootPath, string contentRootPath)
    {
        WebRootPath = webRootPath;
        ContentRootPath = contentRootPath;
    }

    public string WebRootPath { get; set; }

    public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();

    public string ApplicationName { get; set; } = "EMS.API.Tests";

    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();

    public string ContentRootPath { get; set; }

    public string EnvironmentName { get; set; } = "Testing";
}
