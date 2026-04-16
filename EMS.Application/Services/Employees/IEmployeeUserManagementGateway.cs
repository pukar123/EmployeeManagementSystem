namespace EMS.Application.Services.Employees;

public interface IEmployeeUserManagementGateway
{
    Task<EmployeeLinkedUserSnapshot?> GetUserByIdAsync(int userId, CancellationToken cancellationToken = default);

    Task<EmployeeLinkedUserSnapshot?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<EmployeeLinkedUserSnapshot> CreateUserAsync(
        CreateEmployeeLinkedUserRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<int>> GetRoleIdsForUserAsync(int userId, CancellationToken cancellationToken = default);

    Task SetRoleIdsForUserAsync(int userId, IReadOnlyList<int> roleIds, CancellationToken cancellationToken = default);
}
