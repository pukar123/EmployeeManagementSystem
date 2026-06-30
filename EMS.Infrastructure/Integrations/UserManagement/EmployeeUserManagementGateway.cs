using EMS.Application.Services.Employees;
using Pukar.Usermanagement.Application.DTOs.UserRoles;
using Pukar.Usermanagement.Application.DTOs.Users;
using Pukar.Usermanagement.Application.Services.UserRoles;
using Pukar.Usermanagement.Application.Services.Users;

namespace EMS.Infrastructure.Integrations.UserManagement;

public sealed class EmployeeUserManagementGateway : IEmployeeUserManagementGateway
{
    private readonly IUserAdminService _users;
    private readonly IUserRoleService _userRoles;

    public EmployeeUserManagementGateway(IUserAdminService users, IUserRoleService userRoles)
    {
        _users = users;
        _userRoles = userRoles;
    }

    public async Task<EmployeeLinkedUserSnapshot?> GetUserByIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(userId, cancellationToken);
        return user is null ? null : ToSnapshot(user);
    }

    public async Task<EmployeeLinkedUserSnapshot?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByEmailAsync(email, cancellationToken);
        return user is null ? null : ToSnapshot(user);
    }

    public async Task<IReadOnlyDictionary<int, EmployeeLinkedUserSnapshot>> GetUsersByIdsAsync(
        IReadOnlyList<int> userIds,
        CancellationToken cancellationToken = default)
    {
        var users = await _users.GetByIdsAsync(userIds, cancellationToken);
        return users.ToDictionary(u => u.Id, ToSnapshot);
    }

    public async Task<EmployeeLinkedUserSnapshot> CreateUserAsync(
        CreateEmployeeLinkedUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var created = await _users.CreateAsync(
            new CreateUserRequestModel
            {
                Email = request.Email,
                UserName = request.UserName,
                Password = request.Password,
                IsActive = request.IsActive,
                MustChangePassword = request.MustChangePassword,
            },
            cancellationToken);

        return ToSnapshot(created);
    }

    public Task<IReadOnlyList<int>> GetRoleIdsForUserAsync(int userId, CancellationToken cancellationToken = default)
        => _userRoles.GetRoleIdsForUserAsync(userId, cancellationToken);

    public Task SetRoleIdsForUserAsync(int userId, IReadOnlyList<int> roleIds, CancellationToken cancellationToken = default)
        => _userRoles.SetUserRolesAsync(
            userId,
            new AssignUserRolesRequestModel { RoleIds = roleIds },
            cancellationToken);

    public async Task DeactivateLinkedUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(userId, cancellationToken);
        if (user is null)
            return;

        await _users.UpdateAsync(
            userId,
            new UpdateUserRequestModel
            {
                Email = user.Email,
                UserName = user.UserName,
                IsActive = false,
            },
            cancellationToken);
    }

    public Task RevokeOperationalAccessAsync(int userId, CancellationToken cancellationToken = default)
        => _userRoles.SetUserRolesAsync(
            userId,
            new AssignUserRolesRequestModel { RoleIds = Array.Empty<int>() },
            cancellationToken);

    public async Task ActivateLinkedUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(userId, cancellationToken);
        if (user is null)
            return;

        await _users.UpdateAsync(
            userId,
            new UpdateUserRequestModel
            {
                Email = user.Email,
                UserName = user.UserName,
                IsActive = true,
            },
            cancellationToken);
    }

    public Task SetLinkedUserPasswordAsync(
        int userId,
        string newPassword,
        bool requirePasswordChange,
        CancellationToken cancellationToken = default)
        => _users.AdminSetPasswordAsync(
            userId,
            new AdminSetPasswordRequestModel
            {
                NewPassword = newPassword,
                RequirePasswordChange = requirePasswordChange,
            },
            cancellationToken);

    private static EmployeeLinkedUserSnapshot ToSnapshot(UserSummaryResponseModel user)
        => new()
        {
            Id = user.Id,
            Email = user.Email,
            IsActive = user.IsActive,
        };
}
