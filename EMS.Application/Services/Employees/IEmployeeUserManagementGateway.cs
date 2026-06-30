using EMS.Application.DTOs.Employee;
using EMS.Application.Services.Employees;
using EMS.Domain.DbModels;
using EMS.Domain.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using Pukar.Shared;

namespace EMS.Application.Services.Employees;

public interface IEmployeeUserManagementGateway
{
    Task<EmployeeLinkedUserSnapshot?> GetUserByIdAsync(int userId, CancellationToken cancellationToken = default);

    Task<EmployeeLinkedUserSnapshot?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<int, EmployeeLinkedUserSnapshot>> GetUsersByIdsAsync(
        IReadOnlyList<int> userIds,
        CancellationToken cancellationToken = default);

    Task<EmployeeLinkedUserSnapshot> CreateUserAsync(
        CreateEmployeeLinkedUserRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<int>> GetRoleIdsForUserAsync(int userId, CancellationToken cancellationToken = default);

    Task SetRoleIdsForUserAsync(int userId, IReadOnlyList<int> roleIds, CancellationToken cancellationToken = default);

    Task DeactivateLinkedUserAsync(int userId, CancellationToken cancellationToken = default);

    Task RevokeOperationalAccessAsync(int userId, CancellationToken cancellationToken = default);

    Task ActivateLinkedUserAsync(int userId, CancellationToken cancellationToken = default);

    Task SetLinkedUserPasswordAsync(int userId, string newPassword, bool requirePasswordChange, CancellationToken cancellationToken = default);
}
