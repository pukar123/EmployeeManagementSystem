using Pukar.Usermanagement.Application.Options;

namespace Pukar.Usermanagement.UnitTests.Application;

/// <summary>B05: a non-positive limit or window must fail startup with a message naming the setting.</summary>
[TestFixture]
public sealed class AuthRateLimitingOptionsValidatorTests
{
    private readonly AuthRateLimitingOptionsValidator _validator = new();

    [Test]
    public void Defaults_AreValid()
    {
        var result = _validator.Validate(null, new AuthRateLimitingOptions());

        Assert.That(result.Succeeded, Is.True, result.FailureMessage);
    }

    [Test]
    public void Defaults_MatchTheDocumentedValues()
    {
        var options = new AuthRateLimitingOptions();

        Assert.Multiple(() =>
        {
            Assert.That(options.Enabled, Is.True);
            AssertLimit(options.SignInPerIp, 20, 60);
            AssertLimit(options.SignInPerEmail, 5, 300);
            AssertLimit(options.RegisterPerIp, 5, 3600);
            AssertLimit(options.RefreshPerIp, 60, 60);
            AssertLimit(options.ForgotPasswordPerIp, 10, 900);
            AssertLimit(options.ForgotPasswordPerEmail, 3, 3600);
            AssertLimit(options.ResetPasswordPerIp, 10, 900);
        });
    }

    [Test]
    public void ZeroPermitLimit_Fails_NamingTheSetting()
    {
        var options = new AuthRateLimitingOptions { SignInPerIp = FixedWindowLimitOptions.Of(0, 60) };

        var result = _validator.Validate(null, options);

        Assert.That(result.Failed, Is.True);
        Assert.That(result.FailureMessage, Does.Contain("AuthRateLimiting:SignInPerIp:PermitLimit"));
    }

    [Test]
    public void ZeroWindow_Fails_NamingTheSetting()
    {
        var options = new AuthRateLimitingOptions { ForgotPasswordPerEmail = FixedWindowLimitOptions.Of(3, 0) };

        var result = _validator.Validate(null, options);

        Assert.That(result.Failed, Is.True);
        Assert.That(result.FailureMessage, Does.Contain("AuthRateLimiting:ForgotPasswordPerEmail:WindowSeconds"));
    }

    [Test]
    public void NegativeValues_FailEvenWhenLimitingIsDisabled()
    {
        var options = new AuthRateLimitingOptions
        {
            Enabled = false,
            RefreshPerIp = FixedWindowLimitOptions.Of(-1, -5),
        };

        var result = _validator.Validate(null, options);

        Assert.That(result.Failed, Is.True);
        Assert.That(result.FailureMessage, Does.Contain("AuthRateLimiting:RefreshPerIp:PermitLimit"));
        Assert.That(result.FailureMessage, Does.Contain("AuthRateLimiting:RefreshPerIp:WindowSeconds"));
    }

    private static void AssertLimit(FixedWindowLimitOptions limit, int permitLimit, int windowSeconds)
    {
        Assert.That(limit.PermitLimit, Is.EqualTo(permitLimit));
        Assert.That(limit.WindowSeconds, Is.EqualTo(windowSeconds));
    }
}
