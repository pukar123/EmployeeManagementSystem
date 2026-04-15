namespace EMS.Application.Services.Authorization;

public sealed class AuthorizationCutoverReadinessSnapshot
{
    public bool UseRoleKeyMappingEnabled { get; init; }

    public bool LegacyFallbackEnabled { get; init; }

    public AuthorizationTelemetrySnapshot Telemetry { get; init; } = new();

    public bool MeetsMismatchThreshold { get; init; }

    public double MismatchThresholdPercent { get; init; }

    public bool ReadyForGoNoGoReview { get; init; }

    public string Recommendation { get; init; } = string.Empty;
}
