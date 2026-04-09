using Pukar.Usermanagement.Domain.DbModels;

namespace Pukar.Usermanagement.Domain.Repositories.Interface;

public interface IUserRepository : IBaseRepository<User>
{
    Task<User?> GetByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default);
}
