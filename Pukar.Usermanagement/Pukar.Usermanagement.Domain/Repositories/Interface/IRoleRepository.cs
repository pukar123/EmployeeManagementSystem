using Pukar.Usermanagement.Domain.DbModels;

namespace Pukar.Usermanagement.Domain.Repositories.Interface;

public interface IRoleRepository : IBaseRepository<Role>
{
    Task<Role?> GetByNormalizedNameAsync(string normalizedName, CancellationToken cancellationToken = default);
}
