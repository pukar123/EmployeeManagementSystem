using EMS.API.Options;
using EMS.Application.Services.Authorization;
using Microsoft.Extensions.Options;

namespace EMS.API.Services;

public sealed class AuthorizationCutoverReadinessReporter : IAuthorizationCutoverReadinessReporter
{
    private const double DefaultMismatchThresholdPercent = 1.0d;

    private readonly IAuthorizationTelemetryReporter _telemetryReporter;
    private readonly IOptionsSnapshot<AuthorizationModeOptions> _authorizationOptions;

    public AuthorizationCutoverReadinessReporter(
        IAuthorizationTelemetryReporter telemetryReporter,
        IOptionsSnapshot<AuthorizationModeOptions> authorizationOptions)
    {
        _telemetryReporter = telemetryReporter;
        _authorizationOptions = authorizationOptions;
    }

    public AuthorizationCutoverReadinessSnapshot GetReadinessSnapshot()
    {
        var options = _authorizationOptions.Value;
        var telemetry = _telemetryReporter.GetSnapshot();

        var meetsThreshold = telemetry.MismatchRatePercent <= DefaultMismatchThresholdPercent;
        var ready = meetsThreshold && options.UseRoleKeyMapping && !options.EnableLegacyRoleIdFallback;

        var recommendation = ready
            ? "Cutover configuration is active and mismatch threshold is satisfied."
            : "Keep monitoring parity and only disable legacy fallback after mismatch threshold stays within target.";

        return new AuthorizationCutoverReadinessSnapshot
        {
            UseRoleKeyMappingEnabled = options.UseRoleKeyMapping,
            LegacyFallbackEnabled = options.EnableLegacyRoleIdFallback,
            Telemetry = telemetry,
            MeetsMismatchThreshold = meetsThreshold,
            MismatchThresholdPercent = DefaultMismatchThresholdPercent,
            ReadyForGoNoGoReview = ready,
            Recommendation = recommendation,
        };
    }
}
