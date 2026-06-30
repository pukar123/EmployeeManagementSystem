using Microsoft.EntityFrameworkCore;
using Pukar.Usermanagement.Application.DTOs.Users;
using Pukar.Usermanagement.Application.Services.Password;
using Pukar.Usermanagement.Domain.DbModels;
using Pukar.Usermanagement.Domain.Repositories.Interface;
using Pukar.Shared;

namespace Pukar.Usermanagement.Application.Services.Users;

public sealed class UserAdminService : IUserAdminService
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;

    public UserAdminService(IUserRepository users, IPasswordHasher passwordHasher)
    {
        _users = users;
        _passwordHasher = passwordHasher;
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

    public async Task<IReadOnlyList<UserSummaryResponseModel>> GetByIdsAsync(
        IReadOnlyList<int> ids,
        CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
            return Array.Empty<UserSummaryResponseModel>();

        var distinct = ids.Distinct().ToList();
        return await _users.GetQueryable()
            .AsNoTracking()
            .Where(u => distinct.Contains(u.Id))
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

    public async Task<UserSummaryResponseModel?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
            return null;

        var normalized = EmailNormalizer.Normalize(email);
        return await _users.GetQueryable()
            .AsNoTracking()
            .Where(u => u.NormalizedEmail == normalized)
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

    public async Task<UserSummaryResponseModel> CreateAsync(CreateUserRequestModel request, CancellationToken cancellationToken = default)
    {
        var email = StringHelper.NormalizeRequired(request.Email);
        if (string.IsNullOrWhiteSpace(request.Password))
            throw new BusinessRuleException("Password is required for new users.");

        var normalized = EmailNormalizer.Normalize(email);
        if (await _users.GetByNormalizedEmailAsync(normalized, cancellationToken) is not null)
            throw new DuplicateEmailException();

        var utcNow = DateTime.UtcNow;
        var user = new User
        {
            Email = email,
            NormalizedEmail = normalized,
            PasswordHash = _passwordHasher.HashPassword(request.Password),
            UserName = StringHelper.NormalizeOptional(request.UserName),
            IsActive = request.IsActive,
            MustChangePassword = request.MustChangePassword,
            CreatedAtUtc = utcNow,
        };

        await _users.AddAsync(user, cancellationToken);
        await _users.SaveChangesAsync(cancellationToken);

        return new UserSummaryResponseModel
        {
            Id = user.Id,
            Email = user.Email,
            UserName = user.UserName,
            IsActive = user.IsActive,
            CreatedAtUtc = user.CreatedAtUtc,
            LastLoginAtUtc = user.LastLoginAtUtc,
        };
    }

    public async Task<UserSummaryResponseModel?> UpdateAsync(int id, UpdateUserRequestModel request, CancellationToken cancellationToken = default)
    {
        var entity = await _users.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return null;

        var email = StringHelper.NormalizeRequired(request.Email);
        var normalized = EmailNormalizer.Normalize(email);

        if (!string.Equals(entity.NormalizedEmail, normalized, StringComparison.Ordinal))
        {
            if (await _users.GetByNormalizedEmailAsync(normalized, cancellationToken) is not null)
                throw new DuplicateEmailException();
        }

        entity.Email = email;
        entity.NormalizedEmail = normalized;
        entity.UserName = StringHelper.NormalizeOptional(request.UserName);
        entity.IsActive = request.IsActive;

        _users.Update(entity);
        await _users.SaveChangesAsync(cancellationToken);

        return new UserSummaryResponseModel
        {
            Id = entity.Id,
            Email = entity.Email,
            UserName = entity.UserName,
            IsActive = entity.IsActive,
            CreatedAtUtc = entity.CreatedAtUtc,
            LastLoginAtUtc = entity.LastLoginAtUtc,
        };
    }

    public async Task<bool> AdminSetPasswordAsync(int id, AdminSetPasswordRequestModel request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.NewPassword))
            throw new BusinessRuleException("New password is required.");

        var entity = await _users.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return false;

        entity.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
        entity.MustChangePassword = request.RequirePasswordChange;
        _users.Update(entity);
        await _users.SaveChangesAsync(cancellationToken);
        return true;
    }
}
