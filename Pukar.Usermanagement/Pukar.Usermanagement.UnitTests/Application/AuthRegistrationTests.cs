using Microsoft.Extensions.Options;
using Moq;
using Pukar.Shared;
using Pukar.Usermanagement.Application.Options;
using Pukar.Usermanagement.Application.Services.Auth;
using Pukar.Usermanagement.Application.Services.Email;
using Pukar.Usermanagement.Application.Services.Jwt;
using Pukar.Usermanagement.Application.Services.Password;
using Pukar.Usermanagement.Contracts.Auth;
using Pukar.Usermanagement.Domain.DbModels;
using Pukar.Usermanagement.Domain.Repositories.Interface;

namespace Pukar.Usermanagement.UnitTests.Application;

/// <summary>B05: self-registration is off unless <c>Registration:AllowSelfRegistration</c> is true.</summary>
[TestFixture]
public sealed class AuthRegistrationTests
{
    [Test]
    public void RegisterAsync_WhenRegistrationIsOff_ThrowsBeforeAnyRepositoryCall()
    {
        // Strict mocks fail the test on any call, so this proves nothing is looked up or saved.
        var users = new Mock<IUserRepository>(MockBehavior.Strict);
        var refreshTokens = new Mock<IRefreshTokenRepository>(MockBehavior.Strict);
        var service = CreateService(users, refreshTokens, allowSelfRegistration: false);

        var ex = Assert.ThrowsAsync<FeatureDisabledException>(() => service.RegisterAsync(
            new RegisterRequestModel { Email = "new@example.com", Password = "Password123!" },
            clientInfo: null));

        Assert.That(ex!.Code, Is.EqualTo("registration_disabled"));
        Assert.That(ex.Message, Is.EqualTo(RegistrationOptions.DisabledMessage));
        users.VerifyNoOtherCalls();
        refreshTokens.VerifyNoOtherCalls();
    }

    [Test]
    public void RegisterAsync_WhenRegistrationIsOff_IgnoresAnInvalidBody()
    {
        var users = new Mock<IUserRepository>(MockBehavior.Strict);
        var service = CreateService(users, new Mock<IRefreshTokenRepository>(MockBehavior.Strict), allowSelfRegistration: false);

        Assert.ThrowsAsync<FeatureDisabledException>(() => service.RegisterAsync(new RegisterRequestModel(), clientInfo: null));
        users.VerifyNoOtherCalls();
    }

    [Test]
    public void RegistrationOptions_DefaultIsOff()
    {
        Assert.That(new RegistrationOptions().AllowSelfRegistration, Is.False);
    }

    [Test]
    public async Task RegisterAsync_WhenRegistrationIsOn_CreatesActiveUserAndIssuesTokens()
    {
        var users = new Mock<IUserRepository>();
        users.Setup(u => u.GetByNormalizedEmailAsync("NEW@EXAMPLE.COM", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        var refreshTokens = new Mock<IRefreshTokenRepository>();
        var service = CreateService(users, refreshTokens, allowSelfRegistration: true);

        var result = await service.RegisterAsync(
            new RegisterRequestModel { Email = " new@example.com ", Password = "Password123!" },
            clientInfo: null);

        users.Verify(u => u.AddAsync(
            It.Is<User>(x => x.NormalizedEmail == "NEW@EXAMPLE.COM" && x.IsActive),
            It.IsAny<CancellationToken>()), Times.Once);
        Assert.That(result.AccessToken, Is.EqualTo("access-token"));
        Assert.That(result.RefreshToken, Is.Not.Empty);
    }

    [Test]
    public void RegisterAsync_WhenRegistrationIsOn_DuplicateEmailStillConflicts()
    {
        var users = new Mock<IUserRepository>();
        users.Setup(u => u.GetByNormalizedEmailAsync("TAKEN@EXAMPLE.COM", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Email = "taken@example.com", NormalizedEmail = "TAKEN@EXAMPLE.COM" });
        var service = CreateService(users, new Mock<IRefreshTokenRepository>(), allowSelfRegistration: true);

        Assert.ThrowsAsync<DuplicateEmailException>(() => service.RegisterAsync(
            new RegisterRequestModel { Email = "taken@example.com", Password = "Password123!" },
            clientInfo: null));
    }

    private static AuthService CreateService(
        Mock<IUserRepository> users,
        Mock<IRefreshTokenRepository> refreshTokens,
        bool allowSelfRegistration)
    {
        var userRoles = new Mock<IUserRoleRepository>();
        userRoles.Setup(r => r.GetRoleNamesForUserAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<string>());

        var jwt = new Mock<IJwtTokenService>();
        var expires = DateTime.UtcNow.AddMinutes(15);
        jwt.Setup(j => j.CreateAccessToken(
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<DateTime>(),
                out expires))
            .Returns("access-token");

        var hasher = new Mock<IPasswordHasher>();
        hasher.Setup(h => h.HashPassword(It.IsAny<string>())).Returns("hash");

        return new AuthService(
            users.Object,
            refreshTokens.Object,
            userRoles.Object,
            new Mock<IPasswordResetTokenRepository>(MockBehavior.Strict).Object,
            hasher.Object,
            Mock.Of<IPasswordPolicyValidator>(),
            new Mock<IEmailSender>(MockBehavior.Strict).Object,
            jwt.Object,
            Options.Create(new JwtTokenOptions()),
            Options.Create(new SmtpOptions()),
            Options.Create(new PasswordResetOptions()),
            Options.Create(new RegistrationOptions { AllowSelfRegistration = allowSelfRegistration }));
    }
}
