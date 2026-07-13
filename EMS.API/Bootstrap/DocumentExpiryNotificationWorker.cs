using EMS.Application.Options;
using EMS.Application.Services.Notifications;
using EMS.Domain.DbModels;
using EMS.Domain.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EMS.API.Bootstrap;

public sealed class DocumentExpiryNotificationWorker : BackgroundService
{
    private static readonly int[] ReminderThresholds = [30, 7, 1];

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DocumentExpiryNotificationWorker> _logger;

    public DocumentExpiryNotificationWorker(
        IServiceProvider serviceProvider,
        ILogger<DocumentExpiryNotificationWorker> logger)
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
                var documents = scope.ServiceProvider.GetRequiredService<IBaseRepository<Document>>();
                var producer = scope.ServiceProvider.GetRequiredService<IEmsNotificationProducer>();

                var maxDays = options.DocumentExpiryReminderDays > 0
                    ? options.DocumentExpiryReminderDays
                    : 30;
                var today = DateTime.UtcNow.Date;
                var horizon = today.AddDays(maxDays);

                var expiringDocuments = await documents.GetQueryable()
                    .AsNoTracking()
                    .Include(d => d.EmployeeDocuments)
                    .Where(d => d.ExpiryDate != null
                                && d.ExpiryDate >= today
                                && d.ExpiryDate <= horizon)
                    .Take(100)
                    .ToListAsync(stoppingToken);

                foreach (var document in expiringDocuments)
                {
                    if (document.ExpiryDate is not DateTime expiryDate)
                        continue;

                    var daysUntilExpiry = (int)(expiryDate.Date - today).TotalDays;
                    if (!ReminderThresholds.Contains(daysUntilExpiry) && daysUntilExpiry != maxDays)
                        continue;

                    var employeeId = document.EmployeeDocuments
                        .FirstOrDefault(ed => ed.IsActive && !ed.IsDeleted)
                        ?.EmployeeId;
                    if (employeeId is not int linkedEmployeeId)
                        continue;

                    await producer.NotifyDocumentExpiringAsync(
                        document,
                        linkedEmployeeId,
                        daysUntilExpiry,
                        stoppingToken);
                }

                var delaySeconds = options.WorkerPollIntervalSeconds > 0 ? options.WorkerPollIntervalSeconds : 60;
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds * 10), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Document expiry notification worker iteration failed.");
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }
    }
}
