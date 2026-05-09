using EMS.Application.DTOs.Leave;

namespace EMS.Application.Services.Leave;

public interface ILeaveRequestService
{
    Task<IReadOnlyList<LeaveRequestResponseModel>> GetByEmployeeAsync(
        int employeeId,
        CancellationToken cancellationToken = default);

    Task<LeaveRequestResponseModel> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<LeaveAdminSummaryResponseModel> GetAdminSummaryAsync(
        int organizationId,
        DateTime asOfDateUtc,
        CancellationToken cancellationToken = default);
    Task<LeaveRequestResponseModel> CreateAsync(CreateLeaveRequestRequestModel request, CancellationToken cancellationToken = default);
    Task<LeaveRequestResponseModel> UpdateAsync(int id, UpdateLeaveRequestRequestModel request, CancellationToken cancellationToken = default);
    Task<LeaveRequestResponseModel> CancelAsync(int id, CancellationToken cancellationToken = default);
}
