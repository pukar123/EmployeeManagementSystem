using EMS.Application.DTOs.Employee;

namespace EMS.Application.Services.Employees;

/// <summary>
/// Ensures the current user may view, manage, access, or export employee records via role capabilities.
/// </summary>
public interface IEmployeeAccessService
{
    Task EnsureCanViewEmployeesAsync(CancellationToken cancellationToken = default);

    Task EnsureCanManageEmployeesAsync(CancellationToken cancellationToken = default);

    Task EnsureCanAccessEmployeesAsync(CancellationToken cancellationToken = default);

    Task EnsureCanExportEmployeesAsync(CancellationToken cancellationToken = default);

    Task<EmployeeAccessCapabilitiesResponseModel> GetMyCapabilitiesAsync(CancellationToken cancellationToken = default);

    Task EnsureCanViewEmployeeProfileAsync(int employeeId, CancellationToken cancellationToken = default);
}

public static class EmployeeAccessMessages
{
    public const string Denied = "Employee access denied.";
}
