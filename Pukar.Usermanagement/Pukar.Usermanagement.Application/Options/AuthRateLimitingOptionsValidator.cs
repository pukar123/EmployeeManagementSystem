using Microsoft.Extensions.Options;

namespace Pukar.Usermanagement.Application.Options;

/// <summary>
/// Rejects a non-positive limit or window so a typo can never silently mean "unlimited" or "blocked".
/// Runs at startup (<c>ValidateOnStart</c>), whether or not limiting is enabled.
/// </summary>
public sealed class AuthRateLimitingOptionsValidator : IValidateOptions<AuthRateLimitingOptions>
{
    public ValidateOptionsResult Validate(string? name, AuthRateLimitingOptions options)
    {
        var failures = new List<string>();
        foreach (var (settingName, limit) in options.AllLimits())
        {
            var prefix = $"{AuthRateLimitingOptions.SectionName}:{settingName}";
            if (limit is null)
            {
                failures.Add($"{prefix} is missing.");
                continue;
            }

            if (limit.PermitLimit < 1)
                failures.Add($"{prefix}:PermitLimit must be at least 1 (was {limit.PermitLimit}).");

            if (limit.WindowSeconds < 1)
                failures.Add($"{prefix}:WindowSeconds must be at least 1 (was {limit.WindowSeconds}).");
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}
