using EMS.Application.DTOs.EmployeePortal;
using EMS.Application.DTOs.Shift;

namespace EMS.Application.Services.EmployeePortal;

public interface IEmployeePortalService
{
    Task<EmployeePortalEligibilityResponseModel> GetEligibilityAsync(CancellationToken cancellationToken = default);

    Task<EmployeePortalResponseModel> GetPortalAsync(CancellationToken cancellationToken = default);

    Task<ShiftResponseModel?> StartShiftAsync(int shiftId, CancellationToken cancellationToken = default);
}
