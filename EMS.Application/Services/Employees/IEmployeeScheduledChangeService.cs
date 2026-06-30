using EMS.Application.DTOs.Employee;

namespace EMS.Application.Services.Employees;

public interface IEmployeeScheduledChangeService
{
    Task<EmployeeScheduledChangeResponseModel> CreateAsync(
        int employeeId,
        CreateEmployeeScheduledChangeRequestModel request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EmployeeScheduledChangeResponseModel>> ListAsync(
        int employeeId,
        CancellationToken cancellationToken = default);

    Task CancelAsync(int employeeId, int changeId, CancellationToken cancellationToken = default);
}

public interface IEmployeeScheduledChangeApplier
{
    Task<int> ApplyDueChangesAsync(CancellationToken cancellationToken = default);
}
