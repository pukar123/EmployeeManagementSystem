using EMS.Application.DTOs.Employee;

namespace EMS.Application.Services.Employees;

public interface IEmployeeIdentityProvisioningService
{
    Task<ProvisionEmployeeUserResponseModel> ProvisionAsync(int employeeId, CancellationToken cancellationToken = default);

    Task<ProvisionEmployeeUserResponseModel> LinkExistingUserAsync(
        int employeeId,
        LinkEmployeeUserRequestModel request,
        CancellationToken cancellationToken = default);

    Task AssignRolesAsync(
        int employeeId,
        AssignEmployeeUserRolesRequestModel request,
        CancellationToken cancellationToken = default);
}
