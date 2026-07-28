using Microsoft.EntityFrameworkCore;
using Pukar.Usermanagement.Domain.Database;
using Pukar.Usermanagement.Domain.DbModels;
using Pukar.Usermanagement.Domain.Repositories.Interface;

namespace Pukar.Usermanagement.Infrastructure.Repositories;

public sealed class ServiceClientRepository : BaseRepository<ServiceClient>, IServiceClientRepository
{
    public ServiceClientRepository(UserManagementDbContext db) : base(db)
    {
    }

    public Task<ServiceClient?> GetByClientIdAsync(string clientId, CancellationToken cancellationToken = default)
        => Context.ServiceClients.FirstOrDefaultAsync(c => c.ClientId == clientId && c.IsActive, cancellationToken);
}
