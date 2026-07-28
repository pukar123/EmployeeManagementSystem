namespace Pukar.Usermanagement.Contracts.Auth;

/// <summary>
/// Stable claim contract shared by Pukar.Usermanagement issuers and EMS consumers.
/// </summary>
public static class AuthContractConstants
{
    public const string ContractVersion = "v1";

    public const string ContractVersionClaimType = "authz_contract_version";

    /// <summary>
    /// Secondary multi-value role claim emitted alongside standard role claims for compatibility.
    /// </summary>
    public const string RolesClaimType = "roles";
}
