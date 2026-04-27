using EMS.Application.DTOs.Leave;

namespace EMS.Application.Services.Leave;

public interface ILeaveAccrualService
{
    Task<LeaveAccrualRunResultResponseModel> RunAccrualAsync(
        RunLeaveAccrualRequestModel request,
        CancellationToken cancellationToken = default);

    Task<LeaveYearResetResultResponseModel> RunLeaveYearResetAsync(
        RunLeaveYearResetRequestModel request,
        CancellationToken cancellationToken = default);
}
