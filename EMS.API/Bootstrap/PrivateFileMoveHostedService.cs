using EMS.API.Options;
using EMS.API.Services;
using Microsoft.Extensions.Options;

namespace EMS.API.Bootstrap;

/// <summary>
/// Runs <see cref="PublicToPrivateFileMover"/> once at start-up when <c>Storage:MovePublicFilesOnStartup</c> is true.
/// A failure is logged and never stops the API: once static files serve only logos, files left in the old public
/// folder are unreachable anyway, and the next start retries.
/// </summary>
public sealed class PrivateFileMoveHostedService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<StorageOptions> _options;
    private readonly ILogger<PrivateFileMoveHostedService> _logger;

    public PrivateFileMoveHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<StorageOptions> options,
        ILogger<PrivateFileMoveHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_options.Value.MovePublicFilesOnStartup)
            return;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var mover = scope.ServiceProvider.GetRequiredService<PublicToPrivateFileMover>();
            var result = await mover.MoveAsync(cancellationToken);

            _logger.LogInformation(
                "Private file move complete: {Moved} moved, {AlreadyPrivate} already private, {PathsUpdated} paths updated, " +
                "{Missing} missing, {Skipped} skipped, {OrphansMoved} orphans moved, {Conflicts} conflicts.",
                result.Moved,
                result.AlreadyPrivate,
                result.PathsUpdated,
                result.Missing,
                result.Skipped,
                result.OrphansMoved,
                result.Conflicts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Private file move failed; the API continues to start. It will run again at the next start.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
