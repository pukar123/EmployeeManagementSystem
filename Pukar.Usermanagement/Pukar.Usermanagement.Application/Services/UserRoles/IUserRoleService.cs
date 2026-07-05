using Pukar.Usermanagement.Contracts.Roles;
using Pukar.Usermanagement.Contracts.UserRoles;

namespace Pukar.Usermanagement.Application.Services.UserRoles;

public interface IUserRoleService
{
    Task<IReadOnlyList<int>> GetRoleIdsForUserAsync(int userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RoleResponseModel>> GetRolesForUserAsync(int userId, CancellationToken cancellationToken = default);

    Task SetUserRolesAsync(int userId, AssignUserRolesRequestModel request, CancellationToken cancellationToken = default);
}
