using Pukar.Usermanagement.Contracts.Roles;

namespace Pukar.Usermanagement.Application.Services.Roles;

public interface IRoleService
{
    Task<IReadOnlyList<RoleMetadataV1ResponseModel>> GetMetadataV1Async(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RoleResponseModel>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<RoleResponseModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<RoleResponseModel> CreateAsync(CreateRoleRequestModel request, CancellationToken cancellationToken = default);

    Task<RoleResponseModel?> UpdateAsync(int id, UpdateRoleRequestModel request, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
