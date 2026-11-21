namespace Pukar.Usermanagement.Application.Options;

/// <summary>
/// Limits for the anonymous sign-in and password-recovery endpoints (section <c>AuthRateLimiting</c>).
/// Per-IP limits use the connection's remote address only; forwarded headers are never trusted.
/// Per-email limits use the normalised email from the request body, across all IPs.
/// </summary>
public sealed class AuthRateLimitingOptions
{
    public const string SectionName = "AuthRateLimiting";

    /// <summary>Master switch. <c>false</c> turns every auth limit off (local troubleshooting only).</summary>
    public bool Enabled { get; set; } = true;

    /// <summary><c>POST /api/auth/login</c>, per client IP.</summary>
    public FixedWindowLimitOptions SignInPerIp { get; set; } = FixedWindowLimitOptions.Of(20, 60);

    /// <summary><c>POST /api/auth/login</c>, per normalised email across all IPs.</summary>
    public FixedWindowLimitOptions SignInPerEmail { get; set; } = FixedWindowLimitOptions.Of(5, 300);

    /// <summary><c>POST /api/auth/register</c>, per client IP (only matters when registration is on).</summary>
    public FixedWindowLimitOptions RegisterPerIp { get; set; } = FixedWindowLimitOptions.Of(5, 3600);

    /// <summary><c>POST /api/auth/refresh</c>, per client IP.</summary>
    public FixedWindowLimitOptions RefreshPerIp { get; set; } = FixedWindowLimitOptions.Of(60, 60);

    /// <summary><c>POST /api/auth/forgot-password</c>, per client IP.</summary>
    public FixedWindowLimitOptions ForgotPasswordPerIp { get; set; } = FixedWindowLimitOptions.Of(10, 900);

    /// <summary><c>POST /api/auth/forgot-password</c>, per normalised email across all IPs.</summary>
    public FixedWindowLimitOptions ForgotPasswordPerEmail { get; set; } = FixedWindowLimitOptions.Of(3, 3600);

    /// <summary>
    /// <c>POST /api/auth/reset-password</c> and <c>POST /api/invitations/accept</c>, per client IP
    /// (separate counters, same settings).
    /// </summary>
    public FixedWindowLimitOptions ResetPasswordPerIp { get; set; } = FixedWindowLimitOptions.Of(10, 900);

    /// <summary>Every limit with its setting name, for validation and documentation.</summary>
    public IEnumerable<(string Name, FixedWindowLimitOptions Limit)> AllLimits()
    {
        yield return (nameof(SignInPerIp), SignInPerIp);
        yield return (nameof(SignInPerEmail), SignInPerEmail);
        yield return (nameof(RegisterPerIp), RegisterPerIp);
        yield return (nameof(RefreshPerIp), RefreshPerIp);
        yield return (nameof(ForgotPasswordPerIp), ForgotPasswordPerIp);
        yield return (nameof(ForgotPasswordPerEmail), ForgotPasswordPerEmail);
        yield return (nameof(ResetPasswordPerIp), ResetPasswordPerIp);
    }
}
