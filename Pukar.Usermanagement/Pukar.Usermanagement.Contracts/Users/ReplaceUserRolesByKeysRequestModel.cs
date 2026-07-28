namespace Pukar.Usermanagement.Contracts.Users;

public sealed class ReplaceUserRolesByKeysRequestModel
{
    public IReadOnlyList<string> NormalizedRoleKeys { get; set; } = Array.Empty<string>();
}
