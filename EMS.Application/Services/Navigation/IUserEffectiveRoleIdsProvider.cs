namespace EMS.Application.Services.Navigation;

/// <summary>
/// Resolves application role ids for a user (backed by Usermanagement <c>UserRole</c> data).
/// </summary>
public interface IUserEffectiveRoleIdsProvider
{
    Task<IReadOnlyList<int>> GetRoleIdsForUserAsync(int userId, CancellationToken cancellationToken = default);
}
