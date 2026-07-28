using EMS.Application.DTOs.Employee;

namespace EMS.Application.Services.Employees;

public interface IEmployeeTransferService
{
    Task<EmployeeResponseModel?> TransferDepartmentAsync(
        int employeeId,
        TransferEmployeeDepartmentRequestModel request,
        CancellationToken cancellationToken = default);

    Task<EmployeeResponseModel?> TransferPositionAsync(
        int employeeId,
        TransferEmployeePositionRequestModel request,
        CancellationToken cancellationToken = default);

    Task<EmployeeResponseModel?> TransferManagerAsync(
        int employeeId,
        TransferEmployeeManagerRequestModel request,
        CancellationToken cancellationToken = default);
}
