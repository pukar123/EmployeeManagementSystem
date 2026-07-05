namespace EMS.Application.Services.Employees;

/// <summary>
/// EMS-owned anti-corruption interface for User Management identity operations.
/// Implementations must use HTTP contracts only — never direct database or in-process UM services.
/// </summary>
public interface IEmployeeUserManagementGateway
{
    Task<EmployeeLinkedUserSnapshot?> GetUserByIdAsync(int userId, CancellationToken cancellationToken = default);

    Task<EmployeeLinkedUserSnapshot?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<int, EmployeeLinkedUserSnapshot>> GetUsersByIdsAsync(
        IReadOnlyList<int> userIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetRoleKeysForUserAsync(int userId, CancellationToken cancellationToken = default);

    Task SetRoleKeysForUserAsync(
        int userId,
        IReadOnlyList<string> normalizedRoleKeys,
        string? idempotencyKey = null,
        CancellationToken cancellationToken = default);

    Task DeactivateLinkedUserAsync(
        int userId,
        string? idempotencyKey = null,
        CancellationToken cancellationToken = default);

    Task RevokeOperationalAccessAsync(
        int userId,
        string? idempotencyKey = null,
        CancellationToken cancellationToken = default);

    Task ActivateLinkedUserAsync(
        int userId,
        string? idempotencyKey = null,
        CancellationToken cancellationToken = default);

    Task RevokeSessionsAsync(
        int userId,
        string? idempotencyKey = null,
        CancellationToken cancellationToken = default);
}
