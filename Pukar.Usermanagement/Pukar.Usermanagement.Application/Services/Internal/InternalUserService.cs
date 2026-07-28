using Pukar.Usermanagement.Application.Services.Users;
using Pukar.Usermanagement.Contracts.Users;
using Pukar.Usermanagement.Domain.Repositories.Interface;
using Pukar.Shared;

namespace Pukar.Usermanagement.Application.Services.Internal;

public sealed class InternalUserService : IInternalUserService
{
    private readonly IUserAdminService _users;
    private readonly IRefreshTokenRepository _refreshTokens;

    public InternalUserService(IUserAdminService users, IRefreshTokenRepository refreshTokens)
    {
        _users = users;
        _refreshTokens = refreshTokens;
    }

    public Task<IReadOnlyList<UserSummaryResponseModel>> BatchLookupAsync(
        IReadOnlyList<int> userIds,
        CancellationToken cancellationToken = default)
        => _users.GetByIdsAsync(userIds, cancellationToken);

    public Task<UserSummaryResponseModel?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        => _users.GetByEmailAsync(email, cancellationToken);

    public async Task<bool> ActivateAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(userId, cancellationToken);
        if (user is null)
            return false;

        await _users.UpdateAsync(
            userId,
            new UpdateUserRequestModel { Email = user.Email, UserName = user.UserName, IsActive = true },
            cancellationToken);
        return true;
    }

    public async Task<bool> DeactivateAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(userId, cancellationToken);
        if (user is null)
            return false;

        await _users.UpdateAsync(
            userId,
            new UpdateUserRequestModel { Email = user.Email, UserName = user.UserName, IsActive = false },
            cancellationToken);
        return true;
    }

    public Task RevokeAllSessionsAsync(int userId, CancellationToken cancellationToken = default)
        => _refreshTokens.RevokeAllForUserAsync(userId, DateTime.UtcNow, cancellationToken);
}
