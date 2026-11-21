using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Pukar.Usermanagement.API.RateLimiting;
using Pukar.Usermanagement.Application.Options;

namespace Pukar.Usermanagement.UnitTests.Api;

/// <summary>
/// B05 review C1: per-email counters are keyed on a fixed-length hash, and blank or over-long emails
/// get no counter, so a caller cannot make the server hold large keys.
/// </summary>
[TestFixture]
public sealed class AuthEmailRateLimiterTests
{
    [Test]
    public void PartitionKey_IsTheSha256HexOfTheTrimmedLowerCaseEmail()
    {
        var expected = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("person@example.com")));

        var key = AuthEmailRateLimiter.PartitionKeyFor("  Person@Example.COM ");

        Assert.That(key, Is.EqualTo(expected));
        Assert.That(key, Has.Length.EqualTo(64));
        Assert.That(key, Does.Not.Contain("person"), "The raw email must never be the key.");
    }

    [Test]
    public void PartitionKey_IsTheSameForCaseAndSpaceVariants()
    {
        Assert.That(
            AuthEmailRateLimiter.PartitionKeyFor("A@X.com"),
            Is.EqualTo(AuthEmailRateLimiter.PartitionKeyFor(" a@x.com ")));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void PartitionKey_IsNullForABlankEmail(string? email)
    {
        Assert.That(AuthEmailRateLimiter.PartitionKeyFor(email), Is.Null);
    }

    [Test]
    public void PartitionKey_IsNullForAnEmailLongerThan320Characters()
    {
        var tooLong = new string('a', 321 - "@x.io".Length) + "@x.io";
        var longest = new string('a', 320 - "@x.io".Length) + "@x.io";

        Assert.That(tooLong, Has.Length.EqualTo(321));
        Assert.That(AuthEmailRateLimiter.PartitionKeyFor(tooLong), Is.Null);
        Assert.That(AuthEmailRateLimiter.PartitionKeyFor(new string('a', 1_000_000)), Is.Null);
        Assert.That(AuthEmailRateLimiter.PartitionKeyFor(longest), Has.Length.EqualTo(64));
    }

    [Test]
    public void Limiter_CountsPerHashedKey()
    {
        using var limiter = new AuthEmailRateLimiter(Options.Create(new AuthRateLimitingOptions
        {
            SignInPerEmail = FixedWindowLimitOptions.Of(2, 300),
        }));
        var key = AuthEmailRateLimiter.PartitionKeyFor("a@x.com")!;
        var other = AuthEmailRateLimiter.PartitionKeyFor("b@x.com")!;

        using var first = limiter.AttemptAcquire(AuthRateLimitPolicies.SignInEmail, key);
        using var second = limiter.AttemptAcquire(AuthRateLimitPolicies.SignInEmail, AuthEmailRateLimiter.PartitionKeyFor(" A@X.COM ")!);
        using var third = limiter.AttemptAcquire(AuthRateLimitPolicies.SignInEmail, key);
        using var otherFirst = limiter.AttemptAcquire(AuthRateLimitPolicies.SignInEmail, other);

        Assert.That(first.IsAcquired, Is.True);
        Assert.That(second.IsAcquired, Is.True);
        Assert.That(third.IsAcquired, Is.False);
        Assert.That(otherFirst.IsAcquired, Is.True);
    }
}
