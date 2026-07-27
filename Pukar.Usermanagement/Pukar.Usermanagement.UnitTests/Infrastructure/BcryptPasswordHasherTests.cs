using Pukar.Usermanagement.Infrastructure.Services;

namespace Pukar.Usermanagement.UnitTests.Infrastructure;

[TestFixture]
public sealed class BcryptPasswordHasherTests
{
    [Test]
    public void VerifyPassword_WithMalformedHash_ReturnsFalse()
    {
        var hasher = new BcryptPasswordHasher();

        var result = hasher.VerifyPassword("Password123!", "legacy-password-hash");

        Assert.That(result, Is.False);
    }
}
