using EMS.Application.Options;
using EMS.Application.Services.Notifications;
using EMS.Domain.DbModels;
using EMS.Domain.Enums;
using EMS.Domain.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EMS.API.Bootstrap;

public sealed class ScheduledChangeReminderWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ScheduledChangeReminderWorker> _logger;

    public ScheduledChangeReminderWorker(
        IServiceProvider serviceProvider,
        ILogger<ScheduledChangeReminderWorker> logger)
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
                    .GetRequiredService<IOptions<EmployeeSchedulingOptions>>()
                    .Value;
                var scheduledChanges = scope.ServiceProvider.GetRequiredService<IBaseRepository<EmployeeScheduledChange>>();
                var producer = scope.ServiceProvider.GetRequiredService<IEmsNotificationProducer>();

                var reminderDays = options.ScheduledChangeReminderDays > 0
                    ? options.ScheduledChangeReminderDays
                    : 7;
                var now = DateTime.UtcNow;
                var horizon = now.AddDays(reminderDays);

                var upcoming = await scheduledChanges.GetQueryable()
                    .AsNoTracking()
                    .Where(c => c.Status == EmployeeScheduledChangeStatus.Pending
                                && c.EffectiveAtUtc > now
                                && c.EffectiveAtUtc <= horizon)
                    .OrderBy(c => c.EffectiveAtUtc)
                    .Take(50)
                    .ToListAsync(stoppingToken);

                foreach (var change in upcoming)
                    await producer.NotifyUpcomingScheduledChangeAsync(change, stoppingToken);

                var delaySeconds = options.WorkerPollIntervalSeconds > 0 ? options.WorkerPollIntervalSeconds : 60;
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Scheduled change reminder worker iteration failed.");
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }
    }
}
