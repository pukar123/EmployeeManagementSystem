using Microsoft.EntityFrameworkCore;
using Pukar.Usermanagement.Domain.Database;
using Pukar.Usermanagement.Domain.DbModels;
using Pukar.Usermanagement.Domain.Repositories.Interface;

namespace Pukar.Usermanagement.Infrastructure.Repositories;

public sealed class RoleRepository : BaseRepository<Role>, IRoleRepository
{
    public RoleRepository(UserManagementDbContext context)
        : base(context)
    {
    }

    public async Task<Role?> GetByNormalizedNameAsync(string normalizedName, CancellationToken cancellationToken = default)
    {
        return await Context.Set<Role>()
            .FirstOrDefaultAsync(r => r.NormalizedName == normalizedName, cancellationToken);
    }
}
