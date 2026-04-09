using Microsoft.EntityFrameworkCore;
using Pukar.Usermanagement.Domain.Database;
using Pukar.Usermanagement.Domain.DbModels;
using Pukar.Usermanagement.Domain.Repositories.Interface;

namespace Pukar.Usermanagement.Infrastructure.Repositories;

public sealed class UserRepository : BaseRepository<User>, IUserRepository
{
    public UserRepository(UserManagementDbContext context)
        : base(context)
    {
    }

    public async Task<User?> GetByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default)
    {
        return await Context.Set<User>()
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);
    }
}
