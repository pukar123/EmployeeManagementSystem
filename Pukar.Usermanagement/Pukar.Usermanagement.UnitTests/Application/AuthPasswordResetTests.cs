using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using Pukar.Shared;
using Pukar.Usermanagement.Application.Options;
using Pukar.Usermanagement.Application.Services.Auth;
using Pukar.Usermanagement.Application.Services.Email;
using Pukar.Usermanagement.Application.Services.Jwt;
using Pukar.Usermanagement.Application.Services.Password;
using Pukar.Usermanagement.Contracts.Auth;
using Pukar.Usermanagement.Domain.Database;
using Pukar.Usermanagement.Domain.DbModels;
using Pukar.Usermanagement.Infrastructure.Repositories;
using Pukar.Usermanagement.Infrastructure.Services;

namespace Pukar.Usermanagement.UnitTests.Application;

[TestFixture]
public sealed class AuthPasswordResetTests
{
    [Test]
    public async Task RequestPasswordResetAsync_CreatesHashedTokenAndSendsLink()
    {
        var service = CreateService(out var db, out var email);
        db.Users.Add(new User
        {
            Email = "person@example.com",
            NormalizedEmail = "PERSON@EXAMPLE.COM",
            PasswordHash = "existing-hash",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        await service.RequestPasswordResetAsync(new ForgotPasswordRequestModel { Email = "person@example.com" });

        var token = db.PasswordResetTokens.Single();
        var message = email.Messages.Single();
        var encodedToken = Regex.Match(message.Body, "reset-password\\?token=([^\\\"]+)").Groups[1].Value;
        var rawToken = Uri.UnescapeDataString(encodedToken);

        Assert.That(rawToken, Is.Not.Empty);
        Assert.That(token.TokenHash, Is.EqualTo(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)))));
        Assert.That(message.ToEmail, Is.EqualTo("person@example.com"));
        Assert.That(message.Body, Does.Contain("This link expires in 30 minutes"));
    }

    [Test]
    public async Task ResetPasswordAsync_ChangesPasswordAndRejectsReuse()
    {
        var service = CreateService(out var db, out _);
        var hasher = new BcryptPasswordHasher();
        const string rawToken = "reset-token-value";
        var user = new User
        {
            Email = "person@example.com",
            NormalizedEmail = "PERSON@EXAMPLE.COM",
            PasswordHash = hasher.HashPassword("OldPassword123!"),
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        db.PasswordResetTokens.Add(new PasswordResetToken
        {
            UserId = user.Id,
            TokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken))),
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(30),
            CreatedAtUtc = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        await service.ResetPasswordAsync(new ResetPasswordRequestModel
        {
            Token = rawToken,
            NewPassword = "NewPassword123!",
        });

        var updated = db.Users.Single(x => x.Id == user.Id);
        Assert.That(hasher.VerifyPassword("NewPassword123!", updated.PasswordHash), Is.True);
        Assert.That(db.PasswordResetTokens.Single().UsedAtUtc, Is.Not.Null);

        var ex = Assert.ThrowsAsync<BusinessRuleException>(() => service.ResetPasswordAsync(new ResetPasswordRequestModel
        {
            Token = rawToken,
            NewPassword = "AnotherPassword123!",
        }));
        Assert.That(ex!.Message, Does.Contain("invalid or expired"));
    }

    [Test]
    public async Task ResetPasswordAsync_EnforcesPasswordPolicy()
    {
        var service = CreateService(out var db, out _);
        var user = new User
        {
            Email = "person@example.com",
            NormalizedEmail = "PERSON@EXAMPLE.COM",
            PasswordHash = "existing-hash",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        const string rawToken = "reset-token-value";
        db.PasswordResetTokens.Add(new PasswordResetToken
        {
            UserId = user.Id,
            TokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken))),
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(30),
            CreatedAtUtc = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        var ex = Assert.ThrowsAsync<BusinessRuleException>(() => service.ResetPasswordAsync(new ResetPasswordRequestModel
        {
            Token = rawToken,
            NewPassword = "short",
        }));

        Assert.That(ex!.Message, Does.Contain("12"));
        Assert.That(db.PasswordResetTokens.Single().UsedAtUtc, Is.Null);
    }

    private static AuthService CreateService(out UserManagementDbContext db, out CapturingEmailSender email)
    {
        var options = new DbContextOptionsBuilder<UserManagementDbContext>()
            .UseInMemoryDatabase($"auth-reset-tests-{Guid.NewGuid()}")
            .Options;
        db = new UserManagementDbContext(options);
        email = new CapturingEmailSender();

        return new AuthService(
            new UserRepository(db),
            new RefreshTokenRepository(db),
            new UserRoleRepository(db),
            new PasswordResetTokenRepository(db),
            new BcryptPasswordHasher(),
            new PasswordPolicyValidator(Options.Create(new PasswordPolicyOptions())),
            email,
            Mock.Of<IJwtTokenService>(),
            Options.Create(new JwtTokenOptions()),
            Options.Create(new SmtpOptions { WebAppBaseUrl = "http://localhost:3000" }),
            Options.Create(new PasswordResetOptions { TokenExpirationMinutes = 30, RequestCooldownSeconds = 0 }));
    }

    private sealed class CapturingEmailSender : IEmailSender
    {
        public List<EmailMessage> Messages { get; } = [];

        public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
        {
            Messages.Add(message);
            return Task.CompletedTask;
        }
    }
}
