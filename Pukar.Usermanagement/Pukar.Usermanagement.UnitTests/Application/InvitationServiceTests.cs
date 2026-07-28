using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using Pukar.Usermanagement.Application.Options;
using Pukar.Usermanagement.Application.Services.Email;
using Pukar.Usermanagement.Application.Services.Invitations;
using Pukar.Usermanagement.Application.Services.Password;
using Pukar.Usermanagement.Application.Services.Users;
using Pukar.Usermanagement.Contracts.Invitations;
using Pukar.Usermanagement.Contracts.Roles;
using Pukar.Usermanagement.Domain.Database;
using Pukar.Usermanagement.Domain.DbModels;
using Pukar.Usermanagement.Infrastructure.Repositories;
using Pukar.Usermanagement.Infrastructure.Services;
using Pukar.Shared;

namespace Pukar.Usermanagement.UnitTests.Application;

[TestFixture]
public class InvitationServiceTests
{
    [Test]
    public async Task CreateOrSendAsync_IsIdempotentForActiveCorrelation()
    {
        var service = CreateService(out var db);
        var request = new CreateInvitationRequestModel
        {
            ExternalCorrelationId = "employee:1",
            RecipientEmail = "invite@example.com",
            RecipientDisplayName = "Invitee",
        };

        var first = await service.CreateOrSendAsync(request);
        var second = await service.CreateOrSendAsync(request);

        Assert.That(second.Id, Is.EqualTo(first.Id));
        Assert.That(db.AccountInvitations.Count(), Is.EqualTo(1));
    }

    [Test]
    public async Task CreateOrSendAsync_RejectsExistingActiveAccount()
    {
        var service = CreateService(out var db);
        db.Users.Add(new User
        {
            Email = "active@example.com",
            NormalizedEmail = "ACTIVE@EXAMPLE.COM",
            UserName = "Active User",
            PasswordHash = "hash",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            LastLoginAtUtc = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        var request = new CreateInvitationRequestModel
        {
            ExternalCorrelationId = "employee:2",
            RecipientEmail = "active@example.com",
        };

        var ex = Assert.ThrowsAsync<ConflictBusinessRuleException>(() => service.CreateOrSendAsync(request));
        Assert.That(ex!.Message, Does.Contain("active account"));
    }

    [Test]
    public async Task CreateOrSendAsync_DoesNotDeactivateExistingAccountWhenLinkingInactiveUser()
    {
        var service = CreateService(out var db);
        var user = new User
        {
            Email = "link@example.com",
            NormalizedEmail = "LINK@EXAMPLE.COM",
            UserName = "Link User",
            PasswordHash = "hash",
            IsActive = false,
            CreatedAtUtc = DateTime.UtcNow,
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        await service.CreateOrSendAsync(new CreateInvitationRequestModel
        {
            ExternalCorrelationId = "employee:3",
            RecipientEmail = "link@example.com",
            LinkExistingInactiveUserId = user.Id,
        });

        var reloaded = db.Users.Single(u => u.Id == user.Id);
        Assert.That(reloaded.IsActive, Is.False);
    }

    [Test]
    public async Task AcceptAsync_RejectsPasswordShorterThanPolicy()
    {
        var service = CreateService(out var db);
        var user = new User
        {
            Email = "accept@example.com",
            NormalizedEmail = "ACCEPT@EXAMPLE.COM",
            UserName = "Accept",
            PasswordHash = "hash",
            IsActive = false,
            CreatedAtUtc = DateTime.UtcNow,
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        const string rawToken = "unit-test-invitation-token-value-1234567890";
        db.AccountInvitations.Add(new AccountInvitation
        {
            UserId = user.Id,
            ExternalCorrelationId = "employee:4",
            RecipientEmail = user.Email,
            TokenHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(rawToken))),
            ExpiresAtUtc = DateTime.UtcNow.AddHours(1),
            CreatedAtUtc = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        var ex = Assert.ThrowsAsync<BusinessRuleException>(() => service.AcceptAsync(new AcceptInvitationRequestModel
        {
            Token = rawToken,
            NewPassword = "short",
        }));

        Assert.That(ex!.Message, Does.Contain("12"));
    }

    [Test]
    public async Task CreateOrSendAsync_RejectsAdministratorEmail()
    {
        var service = CreateService(out var db);
        var adminRole = new Role
        {
            Name = WellKnownRoles.Admin,
            NormalizedName = WellKnownRoles.AdminNormalizedName,
            IsSystem = true,
        };
        db.Roles.Add(adminRole);
        var adminUser = new User
        {
            Email = "admin@example.com",
            NormalizedEmail = "ADMIN@EXAMPLE.COM",
            UserName = "Admin",
            PasswordHash = "hash",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
        };
        db.Users.Add(adminUser);
        await db.SaveChangesAsync();
        db.UserRoles.Add(new UserRole { UserId = adminUser.Id, RoleId = adminRole.Id });
        await db.SaveChangesAsync();

        var ex = Assert.ThrowsAsync<ConflictBusinessRuleException>(() => service.CreateOrSendAsync(new CreateInvitationRequestModel
        {
            ExternalCorrelationId = "employee:5",
            RecipientEmail = "admin@example.com",
        }));

        Assert.That(ex!.Message, Does.Contain("administrator"));
    }

    [Test]
    public async Task CreateOrSendAsync_WhenSmtpFails_PersistsUserAndReturnsFailedStatus()
    {
        var failingEmail = new Mock<IEmailSender>();
        failingEmail
            .Setup(x => x.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("SMTP down"));
        var service = CreateService(out var db, failingEmail.Object);

        var result = await service.CreateOrSendAsync(new CreateInvitationRequestModel
        {
            ExternalCorrelationId = "employee:6",
            RecipientEmail = "smtp-fail@example.com",
            RecipientDisplayName = "SMTP Fail",
        });

        // Persistence succeeded, so the caller must still receive the user/invitation ids.
        Assert.That(result.UserId, Is.GreaterThan(0));
        Assert.That(result.Id, Is.GreaterThan(0));
        Assert.That(result.DeliveryStatus, Is.EqualTo(InvitationDeliveryStatus.Failed));
        Assert.That(db.Users.Any(u => u.Id == result.UserId), Is.True);
        Assert.That(db.AccountInvitations.Count(), Is.EqualTo(1));
    }

    [Test]
    public async Task ResendAsync_AfterSmtpFailure_RemainsRecoverableAndDoesNotThrow()
    {
        var failingEmail = new Mock<IEmailSender>();
        failingEmail
            .Setup(x => x.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("SMTP down"));
        var service = CreateService(out var db, failingEmail.Object);

        var created = await service.CreateOrSendAsync(new CreateInvitationRequestModel
        {
            ExternalCorrelationId = "employee:7",
            RecipientEmail = "resend@example.com",
        });

        // Resend must not throw after a delivery failure; it stays retryable.
        var resent = await service.ResendAsync(created.Id);
        Assert.That(resent.DeliveryStatus, Is.EqualTo(InvitationDeliveryStatus.Failed));
        Assert.That(db.AccountInvitations.Single().Id, Is.EqualTo(created.Id));
    }

    private static InvitationService CreateService(out UserManagementDbContext db)
    {
        var email = new Mock<IEmailSender>();
        email.Setup(x => x.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        return CreateService(out db, email.Object);
    }

    private static InvitationService CreateService(out UserManagementDbContext db, IEmailSender emailSender)
    {
        var options = new DbContextOptionsBuilder<UserManagementDbContext>()
            .UseInMemoryDatabase($"invitation-tests-{Guid.NewGuid()}")
            .Options;
        db = new UserManagementDbContext(options);

        var users = new UserAdminService(new UserRepository(db), new BcryptPasswordHasher());

        return new InvitationService(
            new AccountInvitationRepository(db),
            users,
            new UserRoleRepository(db),
            emailSender,
            new PasswordPolicyValidator(Options.Create(new PasswordPolicyOptions())),
            Options.Create(new SmtpOptions { WebAppBaseUrl = "http://localhost:3000" }));
    }
}
