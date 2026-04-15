namespace EMS.Application.Services.Authorization;

public interface IAuthorizationModeResolver
{
    bool UseRoleKeyMapping { get; }

    bool EnableLegacyRoleIdFallback { get; }
}
