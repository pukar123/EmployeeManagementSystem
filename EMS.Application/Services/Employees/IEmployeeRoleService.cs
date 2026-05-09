using EMS.Application.DTOs.Employee;

namespace EMS.Application.Services.Employees;

public interface IEmployeeRoleService
{
    Task<IReadOnlyList<EmployeeEffectiveRoleResponseModel>?> GetEffectiveRolesAsync(int employeeId, CancellationToken cancellationToken = default);
    Task<bool> SetDirectRolesAsync(int employeeId, SetEmployeeDirectRolesRequestModel request, CancellationToken cancellationToken = default);
}
