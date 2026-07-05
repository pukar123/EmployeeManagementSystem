using Pukar.Usermanagement.Domain.DbModels;

namespace Pukar.Usermanagement.Domain.Repositories.Interface;

public interface IServiceClientRepository : IBaseRepository<ServiceClient>
{
    Task<ServiceClient?> GetByClientIdAsync(string clientId, CancellationToken cancellationToken = default);
}
