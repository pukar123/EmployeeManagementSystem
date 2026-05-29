using EMS.Application.DTOs.EmployeePortal;
using EMS.Application.DTOs.Shift;

namespace EMS.Application.Services.EmployeePortal;

public interface IEmployeePortalService
{
    Task<EmployeePortalResponseModel> GetPortalAsync(CancellationToken cancellationToken = default);

    Task<ShiftResponseModel?> StartShiftAsync(int shiftId, CancellationToken cancellationToken = default);
}
