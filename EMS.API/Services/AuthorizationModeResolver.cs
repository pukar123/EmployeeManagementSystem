using EMS.API.Options;
using EMS.Application.Services.Authorization;
using Microsoft.Extensions.Options;

namespace EMS.API.Services;

public sealed class AuthorizationModeResolver : IAuthorizationModeResolver
{
    private readonly AuthorizationModeOptions _options;

    public AuthorizationModeResolver(IOptionsSnapshot<AuthorizationModeOptions> options)
    {
        _options = options.Value;
    }

    public bool UseRoleKeyMapping => _options.UseRoleKeyMapping;

    public bool EnableLegacyRoleIdFallback => _options.EnableLegacyRoleIdFallback;
}
