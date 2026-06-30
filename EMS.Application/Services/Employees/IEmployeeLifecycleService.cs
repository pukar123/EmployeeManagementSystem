using EMS.Application.DTOs.Employee;

namespace EMS.Application.Services.Employees;

public interface IEmployeeLifecycleService
{
    Task<EmployeeResponseModel?> TerminateAsync(int id, TerminateEmployeeRequestModel request, CancellationToken cancellationToken = default);
    Task<EmployeeResponseModel?> ArchiveAsync(int id, ArchiveEmployeeRequestModel request, CancellationToken cancellationToken = default);
    Task<EmployeeResponseModel?> RestoreAsync(int id, CancellationToken cancellationToken = default);
    Task<EmployeeResponseModel?> ChangeEmploymentStatusAsync(int id, ChangeEmploymentStatusRequestModel request, CancellationToken cancellationToken = default);
}
