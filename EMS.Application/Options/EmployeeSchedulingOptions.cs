namespace EMS.Application.Options;

public sealed class EmployeeSchedulingOptions
{
    public const string SectionName = "EmployeeScheduling";

    public string BusinessTimeZoneId { get; set; } = "Australia/Sydney";

    public int WorkerPollIntervalSeconds { get; set; } = 60;

    public int ScheduledChangeReminderDays { get; set; } = 7;

    public int DocumentExpiryReminderDays { get; set; } = 30;
}
