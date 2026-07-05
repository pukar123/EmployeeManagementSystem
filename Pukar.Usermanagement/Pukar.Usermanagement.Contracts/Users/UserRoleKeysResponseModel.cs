namespace Pukar.Usermanagement.Contracts.Users;

public sealed class UserRoleKeysResponseModel
{
    public IReadOnlyList<string> NormalizedRoleKeys { get; set; } = Array.Empty<string>();
}
