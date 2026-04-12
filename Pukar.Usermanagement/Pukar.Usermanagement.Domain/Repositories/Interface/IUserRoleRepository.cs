namespace Pukar.Usermanagement.Domain.Repositories.Interface;

public interface IUserRoleRepository
{
    Task<IReadOnlyList<int>> GetRoleIdsForUserAsync(int userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetRoleNamesForUserAsync(int userId, CancellationToken cancellationToken = default);

    Task ReplaceRolesForUserAsync(int userId, IReadOnlyList<int> roleIds, CancellationToken cancellationToken = default);
}
