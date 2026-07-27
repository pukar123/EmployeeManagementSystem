using EMS.Application.Services.Employees;
using Pukar.Usermanagement.Application.Services.Internal;
using Pukar.Usermanagement.Contracts.Users;

namespace EMS.Infrastructure.Integrations.UserManagement;

/// <summary>
/// In-process anti-corruption adapter that fulfils the EMS-owned
/// <see cref="IEmployeeUserManagementGateway"/> by calling User Management application
/// services directly within the same host. It never issues HTTP calls, never talks to
/// UM controllers, and never touches <c>UserManagementDbContext</c> directly — all access
/// flows through UM application services which own their repositories and transactions.
/// </summary>
public sealed class InProcessEmployeeUserManagementGateway : IEmployeeUserManagementGateway
{
    private readonly IInternalUserService _users;
    private readonly IInternalRoleService _roles;

    public InProcessEmployeeUserManagementGateway(
        IInternalUserService users,
        IInternalRoleService roles)
    {
        _users = users;
        _roles = roles;
    }

    public async Task<EmployeeLinkedUserSnapshot?> GetUserByIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        var users = await _users.BatchLookupAsync(new[] { userId }, cancellationToken);
        var match = users.FirstOrDefault(u => u.Id == userId);
        return match is null ? null : ToSnapshot(match);
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
        if (userIds.Count == 0)
            return new Dictionary<int, EmployeeLinkedUserSnapshot>();

        var users = await _users.BatchLookupAsync(userIds, cancellationToken);
        return users
            .Select(ToSnapshot)
            .ToDictionary(u => u.Id);
    }

    public Task<IReadOnlyList<string>> GetRoleKeysForUserAsync(int userId, CancellationToken cancellationToken = default)
        => _roles.GetUserRoleKeysAsync(userId, cancellationToken);

    public Task SetRoleKeysForUserAsync(
        int userId,
        IReadOnlyList<string> normalizedRoleKeys,
        string? idempotencyKey = null,
        CancellationToken cancellationToken = default)
    {
        var keys = normalizedRoleKeys
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Select(k => k.Trim().ToUpperInvariant())
            .Distinct()
            .ToList();
        return _roles.ReplaceUserRolesByKeysAsync(userId, keys, cancellationToken);
    }

    public Task DeactivateLinkedUserAsync(
        int userId,
        string? idempotencyKey = null,
        CancellationToken cancellationToken = default)
        => _users.DeactivateAsync(userId, cancellationToken);

    public Task RevokeOperationalAccessAsync(
        int userId,
        string? idempotencyKey = null,
        CancellationToken cancellationToken = default)
        => SetRoleKeysForUserAsync(userId, Array.Empty<string>(), idempotencyKey, cancellationToken);

    public Task ActivateLinkedUserAsync(
        int userId,
        string? idempotencyKey = null,
        CancellationToken cancellationToken = default)
        => _users.ActivateAsync(userId, cancellationToken);

    public Task RevokeSessionsAsync(
        int userId,
        string? idempotencyKey = null,
        CancellationToken cancellationToken = default)
        => _users.RevokeAllSessionsAsync(userId, cancellationToken);

    private static EmployeeLinkedUserSnapshot ToSnapshot(UserSummaryResponseModel user)
        => new()
        {
            Id = user.Id,
            Email = user.Email,
            IsActive = user.IsActive,
        };
}
