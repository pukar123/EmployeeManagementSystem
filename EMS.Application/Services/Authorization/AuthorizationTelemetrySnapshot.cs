namespace EMS.Application.Services.Authorization;

public sealed class AuthorizationTelemetrySnapshot
{
    public long RoleKeyPathUsedTotal { get; init; }

    public long LegacyFallbackUsedTotal { get; init; }

    public long RoleKeyLegacyMismatchTotal { get; init; }

    public double MismatchRatePercent { get; init; }
}
