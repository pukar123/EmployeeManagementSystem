namespace Pukar.Usermanagement.Application.Options;

/// <summary>One fixed-window limit: at most <see cref="PermitLimit"/> requests per <see cref="WindowSeconds"/>.</summary>
public sealed class FixedWindowLimitOptions
{
    public int PermitLimit { get; set; }

    public int WindowSeconds { get; set; }

    public TimeSpan Window => TimeSpan.FromSeconds(WindowSeconds);

    public static FixedWindowLimitOptions Of(int permitLimit, int windowSeconds)
        => new() { PermitLimit = permitLimit, WindowSeconds = windowSeconds };
}
