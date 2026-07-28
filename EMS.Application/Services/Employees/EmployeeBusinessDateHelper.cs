using EMS.Application.Options;
using Microsoft.Extensions.Options;
using Pukar.Shared;

namespace EMS.Application.Services.Employees;

public interface IEmployeeBusinessDateHelper
{
    DateTime GetBusinessTodayStartUtc();

    bool IsFutureBusinessDate(DateTime effectiveAtUtc);

    void EnsureNotFutureForImmediateAction(DateTime effectiveAtUtc, string actionName);

    void EnsureFutureForScheduledAction(DateTime effectiveAtUtc);
}

public sealed class EmployeeBusinessDateHelper : IEmployeeBusinessDateHelper
{
    private readonly EmployeeSchedulingOptions _options;

    public EmployeeBusinessDateHelper(IOptions<EmployeeSchedulingOptions> options)
    {
        _options = options.Value;
    }

    public DateTime GetBusinessTodayStartUtc()
    {
        var tz = ResolveTimeZone();
        var localNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
        var localToday = localNow.Date;
        return TimeZoneInfo.ConvertTimeToUtc(localToday, tz);
    }

    public bool IsFutureBusinessDate(DateTime effectiveAtUtc)
    {
        var effective = effectiveAtUtc.ToUniversalTime();
        return effective.Date > GetBusinessTodayStartUtc().Date;
    }

    public void EnsureNotFutureForImmediateAction(DateTime effectiveAtUtc, string actionName)
    {
        if (IsFutureBusinessDate(effectiveAtUtc))
        {
            throw new BusinessRuleException(
                $"Future effective dates for {actionName} must be scheduled via POST /api/Employees/{{id}}/scheduled-changes.");
        }
    }

    public void EnsureFutureForScheduledAction(DateTime effectiveAtUtc)
    {
        if (!IsFutureBusinessDate(effectiveAtUtc))
        {
            throw new BusinessRuleException("Scheduled changes require a future effective date in the business timezone.");
        }
    }

    private TimeZoneInfo ResolveTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(_options.BusinessTimeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Australia/Sydney");
        }
    }
}
