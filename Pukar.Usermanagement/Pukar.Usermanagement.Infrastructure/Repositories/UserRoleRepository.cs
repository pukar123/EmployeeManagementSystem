using Microsoft.EntityFrameworkCore;
using Pukar.Usermanagement.Domain.Database;
using Pukar.Usermanagement.Domain.DbModels;
using Pukar.Usermanagement.Domain.Repositories.Interface;

namespace Pukar.Usermanagement.Infrastructure.Repositories;

public sealed class UserRoleRepository : IUserRoleRepository
{
    private readonly UserManagementDbContext _context;

    public UserRoleRepository(UserManagementDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<int>> GetRoleIdsForUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _context.UserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.RoleId)
            .ToListAsync(cancellationToken);
    }

    public async Task ReplaceRolesForUserAsync(int userId, IReadOnlyList<int> roleIds, CancellationToken cancellationToken = default)
    {
        var existing = await _context.UserRoles.Where(ur => ur.UserId == userId).ToListAsync(cancellationToken);
        _context.UserRoles.RemoveRange(existing);

        foreach (var roleId in roleIds.Distinct())
        {
            await _context.UserRoles.AddAsync(new UserRole { UserId = userId, RoleId = roleId }, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
