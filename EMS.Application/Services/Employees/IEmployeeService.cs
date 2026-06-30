using EMS.Application.DTOs.Employee;

namespace EMS.Application.Services.Employees;

public interface IEmployeeService
{
    Task<EmployeeResponseModel> CreateAsync(CreateEmployeeRequestModel request, CancellationToken cancellationToken = default);
    Task<EmployeeResponseModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EmployeeResponseModel>> GetAllAsync(bool includeArchived = false, CancellationToken cancellationToken = default);
    Task<EmployeeProfileResponseModel?> GetProfileAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PossibleDuplicateEmployeeModel>> FindPossibleDuplicatesAsync(
        int organizationId,
        string? email,
        string? firstName,
        string? lastName,
        string? phoneNumber,
        DateTime? dateOfBirth,
        CancellationToken cancellationToken = default);
    Task<EmployeeResponseModel?> UpdateAsync(int id, UpdateEmployeeRequestModel request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, ArchiveEmployeeRequestModel? request = null, CancellationToken cancellationToken = default);
    Task<EmployeeHistoryResponseModel?> GetHistoryAsync(int employeeId, CancellationToken cancellationToken = default);
}
