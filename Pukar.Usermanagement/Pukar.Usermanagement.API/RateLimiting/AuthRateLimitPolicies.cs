using Pukar.Usermanagement.Application.Options;

namespace Pukar.Usermanagement.API.RateLimiting;

/// <summary>
/// Names of the auth rate-limit policies and the setting each one reads.
/// Per-IP policies run in the ASP.NET Core rate limiter middleware; per-email policies run in
/// <see cref="Filters.EmailRateLimitAttribute"/> after model binding.
/// </summary>
public static class AuthRateLimitPolicies
{
    public const string SignIn = "auth-sign-in";
    public const string SignInEmail = "auth-sign-in-email";
    public const string Register = "auth-register";
    public const string Refresh = "auth-refresh";
    public const string ForgotPassword = "auth-forgot-password";
    public const string ForgotPasswordEmail = "auth-forgot-password-email";
    public const string ResetPassword = "auth-reset-password";
    public const string AcceptInvitation = "auth-accept-invitation";

    /// <summary>Policies partitioned by client IP (middleware).</summary>
    public static IReadOnlyList<string> PerIpPolicies { get; } =
        [SignIn, Register, Refresh, ForgotPassword, ResetPassword, AcceptInvitation];

    /// <summary>Policies partitioned by normalised email (action filter).</summary>
    public static IReadOnlyList<string> PerEmailPolicies { get; } = [SignInEmail, ForgotPasswordEmail];

    /// <summary>The configured limit for <paramref name="policyName"/>.</summary>
    public static FixedWindowLimitOptions LimitFor(string policyName, AuthRateLimitingOptions options)
        => policyName switch
        {
            SignIn => options.SignInPerIp,
            SignInEmail => options.SignInPerEmail,
            Register => options.RegisterPerIp,
            Refresh => options.RefreshPerIp,
            ForgotPassword => options.ForgotPasswordPerIp,
            ForgotPasswordEmail => options.ForgotPasswordPerEmail,
            ResetPassword => options.ResetPasswordPerIp,
            AcceptInvitation => options.ResetPasswordPerIp,
            _ => throw new ArgumentOutOfRangeException(nameof(policyName), policyName, "Unknown auth rate-limit policy."),
        };
}
