using Pukar.Usermanagement.Domain.Repositories.Interface;

namespace Pukar.Usermanagement.Host.Bootstrap;

public sealed class IdempotencyCleanupHostedService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<IdempotencyCleanupHostedService> _logger;

    public IdempotencyCleanupHostedService(
        IServiceProvider serviceProvider,
        ILogger<IdempotencyCleanupHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var records = scope.ServiceProvider.GetRequiredService<IIdempotencyRecordRepository>();
                await records.DeleteExpiredAsync(DateTime.UtcNow, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Idempotency record cleanup failed.");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }
}
