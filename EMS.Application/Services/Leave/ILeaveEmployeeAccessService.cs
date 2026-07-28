namespace EMS.Application.Services.Leave;

/// <summary>
/// Ensures the current user may read or mutate leave data for the given employee (self-linked or HR via Leave menu).
/// </summary>
public interface ILeaveEmployeeAccessService
{
    /// <summary>
    /// Throws <c>BusinessRuleException</c> with message <see cref="LeaveAccessMessages.Denied"/> if access is not allowed.
    /// </summary>
    Task EnsureCanAccessEmployeeForLeaveAsync(int employeeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// True when the user has the main Leave menu permission and may act on behalf of other employees.
    /// </summary>
    Task<bool> CanManageOtherEmployeesLeaveAsync(CancellationToken cancellationToken = default);
}

public static class LeaveAccessMessages
{
    public const string Denied = "Leave access denied.";
}
