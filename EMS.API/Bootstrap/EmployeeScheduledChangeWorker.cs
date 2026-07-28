using EMS.Application.Services.Employees;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EMS.API.Bootstrap;

public sealed class EmployeeScheduledChangeWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EmployeeScheduledChangeWorker> _logger;

    public EmployeeScheduledChangeWorker(
        IServiceProvider serviceProvider,
        ILogger<EmployeeScheduledChangeWorker> logger)
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
                var options = scope.ServiceProvider
                    .GetRequiredService<Microsoft.Extensions.Options.IOptions<EMS.Application.Options.EmployeeSchedulingOptions>>()
                    .Value;
                var applier = scope.ServiceProvider.GetRequiredService<IEmployeeScheduledChangeApplier>();
                var applied = await applier.ApplyDueChangesAsync(stoppingToken);
                if (applied > 0)
                    _logger.LogInformation("Applied {Count} scheduled employee changes.", applied);

                var delaySeconds = options.WorkerPollIntervalSeconds > 0 ? options.WorkerPollIntervalSeconds : 60;
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Employee scheduled change worker iteration failed.");
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }
    }
}
