namespace EMS.Application.Services.Employees;

public interface IEmployeeRoleSyncService
{
    Task SyncEmployeeAsync(int employeeId, CancellationToken cancellationToken = default);
    Task SyncEmployeesForPositionAsync(int jobPositionId, CancellationToken cancellationToken = default);
}
