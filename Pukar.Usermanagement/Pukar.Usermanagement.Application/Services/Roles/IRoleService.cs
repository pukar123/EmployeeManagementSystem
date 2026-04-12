using Pukar.Usermanagement.Application.DTOs.Roles;

namespace Pukar.Usermanagement.Application.Services.Roles;

public interface IRoleService
{
    Task<IReadOnlyList<RoleResponseModel>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<RoleResponseModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<RoleResponseModel> CreateAsync(CreateRoleRequestModel request, CancellationToken cancellationToken = default);

    Task<RoleResponseModel?> UpdateAsync(int id, UpdateRoleRequestModel request, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
