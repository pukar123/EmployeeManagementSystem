namespace Pukar.Usermanagement.Contracts.Users;

public sealed class BatchUserLookupRequestModel
{
    public IReadOnlyList<int> UserIds { get; set; } = Array.Empty<int>();
}
