using Pukar.Usermanagement.Contracts.Roles;
using Pukar.Usermanagement.Contracts.Users;

namespace Pukar.Usermanagement.Application.Services.Internal;

public interface IInternalRoleService
{
    Task<IReadOnlyList<RoleMetadataV1ResponseModel>> GetMetadataAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetUserRoleKeysAsync(int userId, CancellationToken cancellationToken = default);

    Task ReplaceUserRolesByKeysAsync(int userId, IReadOnlyList<string> normalizedRoleKeys, CancellationToken cancellationToken = default);
}
