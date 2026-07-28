using Pukar.Usermanagement.Domain.Repositories.Interface;

namespace EMS.API.Bootstrap;

/// <summary>
/// Periodically prunes expired User Management idempotency records. Retained for the internal
/// service-token compatibility endpoints hosted in-process by EMS.API.
/// </summary>
public sealed class UserManagementIdempotencyCleanupHostedService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<UserManagementIdempotencyCleanupHostedService> _logger;

    public UserManagementIdempotencyCleanupHostedService(
        IServiceProvider serviceProvider,
        ILogger<UserManagementIdempotencyCleanupHostedService> logger)
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
