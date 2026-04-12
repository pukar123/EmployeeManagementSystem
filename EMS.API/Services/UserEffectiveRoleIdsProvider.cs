using EMS.Application.Services.Navigation;
using Pukar.Usermanagement.Domain.Repositories.Interface;

namespace EMS.API.Services;

public sealed class UserEffectiveRoleIdsProvider : IUserEffectiveRoleIdsProvider
{
    private readonly IUserRoleRepository _userRoles;

    public UserEffectiveRoleIdsProvider(IUserRoleRepository userRoles)
    {
        _userRoles = userRoles;
    }

    public Task<IReadOnlyList<int>> GetRoleIdsForUserAsync(int userId, CancellationToken cancellationToken = default) =>
        _userRoles.GetRoleIdsForUserAsync(userId, cancellationToken);
}
