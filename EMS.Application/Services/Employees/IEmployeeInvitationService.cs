using EMS.Application.DTOs.Employee;

namespace EMS.Application.Services.Employees;

public interface IEmployeeInvitationService
{
    Task<EmployeeInvitationResponseModel> SendAsync(int employeeId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EmployeeInvitationResponseModel>> ListAsync(int employeeId, CancellationToken cancellationToken = default);

    Task RevokeAsync(int employeeId, int invitationId, CancellationToken cancellationToken = default);

    Task AcceptAsync(AcceptEmployeeInvitationRequestModel request, CancellationToken cancellationToken = default);
}
