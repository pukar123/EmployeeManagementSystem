namespace EMS.Application.Services.Authorization;

public sealed class UserIdentitySnapshot
{
    public int? UserId { get; init; }

    public string? Email { get; init; }

    public string? UserName { get; init; }

    public IReadOnlyList<string> RoleKeys { get; init; } = Array.Empty<string>();
}
