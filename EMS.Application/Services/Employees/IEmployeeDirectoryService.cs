using EMS.Application.DTOs.Employee;

namespace EMS.Application.Services.Employees;

public interface IEmployeeDirectoryService
{
    Task<PagedEmployeeDirectoryResponseModel> QueryAsync(EmployeeDirectoryQueryModel query, CancellationToken cancellationToken = default);
    Task<EmployeeExportFileResponseModel> ExportCsvAsync(EmployeeDirectoryQueryModel query, CancellationToken cancellationToken = default);
}
