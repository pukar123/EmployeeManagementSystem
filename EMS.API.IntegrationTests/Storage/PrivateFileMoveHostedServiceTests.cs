using EMS.API.Bootstrap;
using EMS.API.Options;
using EMS.API.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace EMS.API.IntegrationTests.Storage;

/// <summary>B08 M8: the start-up move runs only when enabled, and a failing move never fails start-up.</summary>
[TestFixture]
public sealed class PrivateFileMoveHostedServiceTests
{
    [Test]
    public async Task M8_SettingOff_DoesNotResolveTheMover()
    {
        var resolved = false;
        using var provider = BuildProvider(() => resolved = true);
        var service = CreateService(provider, enabled: false);

        await service.StartAsync(CancellationToken.None);

        Assert.That(resolved, Is.False);
    }

    [Test]
    public void M8_SettingOn_MoverThatThrows_DoesNotFailStart()
    {
        var resolved = false;
        using var provider = BuildProvider(() => resolved = true);
        var service = CreateService(provider, enabled: true);

        Assert.DoesNotThrowAsync(() => service.StartAsync(CancellationToken.None));
        Assert.That(resolved, Is.True, "With the setting on, the mover is used.");
    }

    private static ServiceProvider BuildProvider(Action onResolve)
    {
        var services = new ServiceCollection();
        services.AddScoped<PublicToPrivateFileMover>(_ =>
        {
            onResolve();
            throw new InvalidOperationException("Simulated mover failure.");
        });
        return services.BuildServiceProvider();
    }

    private static PrivateFileMoveHostedService CreateService(ServiceProvider provider, bool enabled)
        => new(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Microsoft.Extensions.Options.Options.Create(new StorageOptions { MovePublicFilesOnStartup = enabled }),
            NullLogger<PrivateFileMoveHostedService>.Instance);
}
