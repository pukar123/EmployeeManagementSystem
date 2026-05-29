using EMS.Application.DTOs.Shift;

namespace EMS.Application.Services.Shifts;

public interface IShiftService
{
    Task<ShiftResponseModel> CreateAsync(CreateShiftRequestModel request, CancellationToken cancellationToken = default);

    Task<ShiftResponseModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ShiftResponseModel>> GetAllAsync(
        int? organizationId,
        int? employeeId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ShiftResponseModel>> GetUpcomingByEmployeeAsync(
        int employeeId,
        DateTime? fromUtc,
        CancellationToken cancellationToken = default);

    Task<ShiftResponseModel?> UpdateAsync(int id, UpdateShiftRequestModel request, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);

    Task<ShiftResponseModel?> StartShiftAsync(int shiftId, int employeeId, CancellationToken cancellationToken = default);
}
