using Microsoft.EntityFrameworkCore;
using Pukar.Usermanagement.Application.DTOs.Users;
using Pukar.Usermanagement.Domain.Repositories.Interface;

namespace Pukar.Usermanagement.Application.Services.Users;

public sealed class UserAdminService : IUserAdminService
{
    private readonly IUserRepository _users;

    public UserAdminService(IUserRepository users)
    {
        _users = users;
    }

    public async Task<IReadOnlyList<UserSummaryResponseModel>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _users.GetQueryable()
            .AsNoTracking()
            .OrderBy(u => u.Email)
            .Select(u => new UserSummaryResponseModel
            {
                Id = u.Id,
                Email = u.Email,
                UserName = u.UserName,
                IsActive = u.IsActive,
                CreatedAtUtc = u.CreatedAtUtc,
                LastLoginAtUtc = u.LastLoginAtUtc,
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<UserSummaryResponseModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _users.GetQueryable()
            .AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new UserSummaryResponseModel
            {
                Id = u.Id,
                Email = u.Email,
                UserName = u.UserName,
                IsActive = u.IsActive,
                CreatedAtUtc = u.CreatedAtUtc,
                LastLoginAtUtc = u.LastLoginAtUtc,
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
}
