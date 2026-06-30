namespace EMS.Application.Services.Employees;

/// <summary>
/// Ensures the current user may view or manage employee records (Employees menu or Admin).
/// </summary>
public interface IEmployeeAccessService
{
    /// <summary>
    /// Throws <c>BusinessRuleException</c> with message <see cref="EmployeeAccessMessages.Denied"/> if view access is not allowed.
    /// </summary>
    Task EnsureCanViewEmployeesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Throws <c>BusinessRuleException</c> with message <see cref="EmployeeAccessMessages.Denied"/> if manage access is not allowed.
    /// </summary>
    Task EnsureCanManageEmployeesAsync(CancellationToken cancellationToken = default);
}

public static class EmployeeAccessMessages
{
    public const string Denied = "Employee access denied.";
}
