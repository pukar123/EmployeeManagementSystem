using EMS.Application.DTOs.Leave;

namespace EMS.Application.Services.Leave;

public interface ILeaveTypeService
{
    Task<IReadOnlyList<LeaveTypeResponseModel>> GetByOrganizationAsync(
        int organizationId,
        CancellationToken cancellationToken = default);

    Task<LeaveTypeResponseModel> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<LeaveTypeResponseModel> CreateAsync(CreateLeaveTypeRequestModel request, CancellationToken cancellationToken = default);
    Task<LeaveTypeResponseModel> UpdateAsync(int id, UpdateLeaveTypeRequestModel request, CancellationToken cancellationToken = default);
}
