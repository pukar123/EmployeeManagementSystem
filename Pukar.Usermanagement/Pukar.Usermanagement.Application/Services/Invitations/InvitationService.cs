using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Pukar.Shared;

using Pukar.Usermanagement.Application.Options;

using Pukar.Usermanagement.Application.Services.Email;

using Pukar.Usermanagement.Application.Services.Password;

using Pukar.Usermanagement.Application.Services.Users;

using Pukar.Usermanagement.Contracts.Invitations;

using Pukar.Usermanagement.Contracts.Roles;

using Pukar.Usermanagement.Contracts.Users;

using Pukar.Usermanagement.Domain.DbModels;

using Pukar.Usermanagement.Domain.Enums;

using Pukar.Usermanagement.Domain.Repositories.Interface;



namespace Pukar.Usermanagement.Application.Services.Invitations;



public sealed class InvitationService : IInvitationService

{

    private const int ResendCooldownSeconds = 60;

    private const int InvitationLifetimeHours = 24;

    private const int TokenByteLength = 32;



    private readonly IAccountInvitationRepository _invitations;

    private readonly IUserAdminService _users;

    private readonly IUserRoleRepository _userRoles;

    private readonly IEmailSender _emailSender;

    private readonly IPasswordPolicyValidator _passwordPolicy;

    private readonly SmtpOptions _smtpOptions;



    public InvitationService(

        IAccountInvitationRepository invitations,

        IUserAdminService users,

        IUserRoleRepository userRoles,

        IEmailSender emailSender,

        IPasswordPolicyValidator passwordPolicy,

        IOptions<SmtpOptions> smtpOptions)

    {

        _invitations = invitations;

        _users = users;

        _userRoles = userRoles;

        _emailSender = emailSender;

        _passwordPolicy = passwordPolicy;

        _smtpOptions = smtpOptions.Value;

    }



    public async Task<InvitationResponseModel> CreateOrSendAsync(

        CreateInvitationRequestModel request,

        CancellationToken cancellationToken = default)

    {

        var correlationId = StringHelper.NormalizeRequired(request.ExternalCorrelationId);

        var email = StringHelper.NormalizeRequired(request.RecipientEmail);



        var active = await _invitations.GetActiveByCorrelationIdAsync(correlationId, cancellationToken);

        if (active is not null)

            return ToResponse(active);



        return await CreateAndSendAsync(correlationId, email, request.RecipientDisplayName, request.LinkExistingInactiveUserId, cancellationToken);

    }



    public async Task<IReadOnlyList<InvitationResponseModel>> ListByCorrelationIdAsync(

        string correlationId,

        CancellationToken cancellationToken = default)

    {

        var items = await _invitations.ListByCorrelationIdAsync(correlationId, cancellationToken);

        return items.Select(ToResponse).ToList();

    }



    public async Task<InvitationResponseModel> ResendAsync(int invitationId, CancellationToken cancellationToken = default)

    {

        var invitation = await _invitations.GetByIdAsync(invitationId, cancellationToken)

            ?? throw new BusinessRuleException("Invitation was not found.");



        if (invitation.UsedAtUtc.HasValue)

            throw new BusinessRuleException("Used invitations cannot be resent.");



        if (invitation.RevokedAtUtc.HasValue)

            throw new BusinessRuleException("Revoked invitations cannot be resent.");



        if (invitation.LastSentAtUtc.HasValue

            && DateTime.UtcNow - invitation.LastSentAtUtc.Value < TimeSpan.FromSeconds(ResendCooldownSeconds))

        {

            throw new BusinessRuleException("Please wait before resending the invitation.");

        }



        return await ResendExistingAsync(invitation, invitation.RecipientEmail, invitation.RecipientDisplayName, cancellationToken);

    }



    public async Task RevokeAsync(int invitationId, CancellationToken cancellationToken = default)

    {

        var invitation = await _invitations.GetByIdAsync(invitationId, cancellationToken)

            ?? throw new BusinessRuleException("Invitation was not found.");



        if (invitation.UsedAtUtc.HasValue)

            throw new BusinessRuleException("Used invitations cannot be revoked.");



        if (invitation.RevokedAtUtc.HasValue)

            return;



        invitation.RevokedAtUtc = DateTime.UtcNow;

        _invitations.Update(invitation);

        try

        {

            await _invitations.SaveChangesAsync(cancellationToken);

        }

        catch (DbUpdateConcurrencyException)

        {

            throw new ConcurrencyConflictException("Invitation was modified concurrently. Please retry.");

        }

    }



    public async Task AcceptAsync(AcceptInvitationRequestModel request, CancellationToken cancellationToken = default)

    {

        if (string.IsNullOrWhiteSpace(request.Token))

            throw new BusinessRuleException("Invitation token is required.");



        _passwordPolicy.ValidateOrThrow(request.NewPassword);



        var tokenHash = HashToken(request.Token.Trim());

        var invitation = await _invitations.GetByTokenHashAsync(tokenHash, cancellationToken)

            ?? throw new BusinessRuleException("Invitation is invalid or has expired.");



        if (invitation.UsedAtUtc.HasValue)

            throw new BusinessRuleException("Invitation has already been used.");



        if (invitation.RevokedAtUtc.HasValue)

            throw new BusinessRuleException("Invitation has been revoked.");



        if (invitation.ExpiresAtUtc < DateTime.UtcNow)

            throw new BusinessRuleException("Invitation has expired.");



        var user = await _users.GetByIdAsync(invitation.UserId, cancellationToken)

            ?? throw new BusinessRuleException("Invitation account is no longer valid.");



        if (user.IsActive && user.LastLoginAtUtc.HasValue)

        {

            throw new ConflictBusinessRuleException(

                "This account is already active. Sign in with your existing password or use account recovery.");

        }



        if (await UserHasAdminRoleAsync(invitation.UserId, cancellationToken))

        {

            throw new ConflictBusinessRuleException(

                "Administrator accounts cannot be activated or have passwords set through invitations.");

        }



        await _users.AdminSetPasswordAsync(

            invitation.UserId,

            new AdminSetPasswordRequestModel { NewPassword = request.NewPassword, RequirePasswordChange = false },

            cancellationToken);



        await _users.UpdateAsync(

            invitation.UserId,

            new UpdateUserRequestModel { Email = user.Email, UserName = user.UserName, IsActive = true },

            cancellationToken);



        var marked = await _invitations.TryMarkUsedAsync(

            invitation.Id,

            invitation.RowVersion,

            DateTime.UtcNow,

            cancellationToken);



        if (!marked)

            throw new BusinessRuleException("Invitation has already been used.");

    }



    public Task RevokePendingByCorrelationIdAsync(string correlationId, CancellationToken cancellationToken = default)

        => _invitations.RevokePendingByCorrelationIdAsync(correlationId, DateTime.UtcNow, cancellationToken);



    private async Task<InvitationResponseModel> CreateAndSendAsync(

        string correlationId,

        string email,

        string? displayName,

        int? linkExistingInactiveUserId,

        CancellationToken cancellationToken)

    {

        var existingUser = await _users.GetByEmailAsync(email, cancellationToken);

        int userId;



        if (existingUser is not null)

        {

            await EnsureExistingUserEligibleForInvitationAsync(existingUser, linkExistingInactiveUserId, cancellationToken);

            userId = existingUser.Id;

        }

        else

        {

            if (linkExistingInactiveUserId.HasValue)

            {

                throw new BusinessRuleException(

                    "LinkExistingInactiveUserId was provided but no account exists for the recipient email.");

            }



            var created = await _users.CreateAsync(

                new CreateUserRequestModel

                {

                    Email = email,

                    UserName = displayName?.Trim(),

                    Password = GenerateUnusablePassword(),

                    IsActive = false,

                    MustChangePassword = false,

                },

                cancellationToken);

            userId = created.Id;

        }



        var rawToken = GenerateToken();

        var now = DateTime.UtcNow;

        var invitation = new AccountInvitation

        {

            UserId = userId,

            ExternalCorrelationId = correlationId,

            RecipientEmail = email,

            RecipientDisplayName = displayName?.Trim(),

            TokenHash = HashToken(rawToken),

            ExpiresAtUtc = now.AddHours(InvitationLifetimeHours),

            DeliveryStatus = AccountInvitationDeliveryStatus.Pending,

            CreatedAtUtc = now,

        };



        await _invitations.AddAsync(invitation, cancellationToken);

        await _invitations.SaveChangesAsync(cancellationToken);



        try

        {

            await SendEmailAsync(email, displayName, rawToken, cancellationToken);

            invitation.DeliveryStatus = AccountInvitationDeliveryStatus.Sent;

            invitation.LastSentAtUtc = DateTime.UtcNow;

            invitation.DeliveryFailureReason = null;

        }

        catch (Exception ex)

        {

            // Persistence already succeeded: the user and invitation exist. Record the
            // delivery failure but DO NOT throw — the caller (e.g. EMS) must still receive
            // the user/invitation id so it can link ExternalIdentityKey and retry delivery
            // later via resend. Throwing here would strand a persisted account.

            invitation.DeliveryStatus = AccountInvitationDeliveryStatus.Failed;

            invitation.DeliveryFailureReason = ex.Message.Length > 2000 ? ex.Message[..2000] : ex.Message;

        }



        _invitations.Update(invitation);

        await _invitations.SaveChangesAsync(cancellationToken);

        return ToResponse(invitation);

    }



    private async Task EnsureExistingUserEligibleForInvitationAsync(

        UserSummaryResponseModel existingUser,

        int? linkExistingInactiveUserId,

        CancellationToken cancellationToken)

    {

        if (await UserHasAdminRoleAsync(existingUser.Id, cancellationToken))

        {

            throw new ConflictBusinessRuleException(

                "An administrator account already exists for this email. Invitations cannot modify administrator accounts.");

        }



        if (existingUser.IsActive && existingUser.LastLoginAtUtc.HasValue)

        {

            throw new ConflictBusinessRuleException(

                "An active account already exists for this email. Use the authenticated account-linking workflow instead of a new invitation.");

        }



        if (!linkExistingInactiveUserId.HasValue)

        {

            throw new ConflictBusinessRuleException(

                "An account already exists for this email. Provide LinkExistingInactiveUserId to link a pre-provisioned inactive account.");

        }



        if (linkExistingInactiveUserId.Value != existingUser.Id)

        {

            throw new ConflictBusinessRuleException(

                "LinkExistingInactiveUserId does not match the existing account for this email.");

        }



        if (existingUser.IsActive)

        {

            throw new ConflictBusinessRuleException(

                "Cannot link invitation to an active account. Deactivate the account through authorized admin workflows first.");

        }

    }



    private async Task<bool> UserHasAdminRoleAsync(int userId, CancellationToken cancellationToken)

    {

        var roleNames = await _userRoles.GetRoleNamesForUserAsync(userId, cancellationToken);

        return roleNames.Any(name =>

            string.Equals(name, WellKnownRoles.Admin, StringComparison.OrdinalIgnoreCase)

            || string.Equals(name, WellKnownRoles.AdminNormalizedName, StringComparison.OrdinalIgnoreCase));

    }



    private async Task<InvitationResponseModel> ResendExistingAsync(

        AccountInvitation invitation,

        string email,

        string? displayName,

        CancellationToken cancellationToken)

    {

        var rawToken = GenerateToken();

        invitation.TokenHash = HashToken(rawToken);

        invitation.ExpiresAtUtc = DateTime.UtcNow.AddHours(InvitationLifetimeHours);

        invitation.DeliveryStatus = AccountInvitationDeliveryStatus.Pending;

        invitation.DeliveryFailureReason = null;

        invitation.RecipientEmail = email;

        invitation.RecipientDisplayName = displayName?.Trim();



        try

        {

            await SendEmailAsync(email, displayName, rawToken, cancellationToken);

            invitation.DeliveryStatus = AccountInvitationDeliveryStatus.Sent;

            invitation.LastSentAtUtc = DateTime.UtcNow;

        }

        catch (Exception ex)

        {

            // Resend rotates the token before delivery; a delivery failure is recorded but
            // not thrown so repeated resends stay recoverable rather than becoming a
            // permanent error. The rotated token was already persisted below.

            invitation.DeliveryStatus = AccountInvitationDeliveryStatus.Failed;

            invitation.DeliveryFailureReason = ex.Message.Length > 2000 ? ex.Message[..2000] : ex.Message;

        }



        _invitations.Update(invitation);

        try

        {

            await _invitations.SaveChangesAsync(cancellationToken);

        }

        catch (DbUpdateConcurrencyException)

        {

            throw new ConcurrencyConflictException("Invitation was modified concurrently. Please retry.");

        }



        return ToResponse(invitation);

    }



    private async Task SendEmailAsync(string email, string? displayName, string rawToken, CancellationToken cancellationToken)

    {

        var baseUrl = _smtpOptions.WebAppBaseUrl.TrimEnd('/');

        var link = $"{baseUrl}/accept-invitation?token={Uri.EscapeDataString(rawToken)}";

        var greeting = string.IsNullOrWhiteSpace(displayName) ? "Hello" : $"Hello {displayName}";

        await _emailSender.SendAsync(

            new EmailMessage

            {

                ToEmail = email,

                Subject = "You're invited to access the system",

                Body = $"""

                    <p>{greeting},</p>

                    <p>You have been invited to create your account.</p>

                    <p><a href="{link}">Accept invitation and set your password</a></p>

                    <p>This link expires in 24 hours and can only be used once.</p>

                    """,

            },

            cancellationToken);

    }



    private static InvitationResponseModel ToResponse(AccountInvitation invitation)

    {

        var cooldownRemaining = 0;

        if (invitation.LastSentAtUtc.HasValue)

        {

            var elapsed = (int)(DateTime.UtcNow - invitation.LastSentAtUtc.Value).TotalSeconds;

            cooldownRemaining = Math.Max(0, ResendCooldownSeconds - elapsed);

        }



        return new InvitationResponseModel

        {

            Id = invitation.Id,

            UserId = invitation.UserId,

            ExternalCorrelationId = invitation.ExternalCorrelationId,

            ExpiresAtUtc = invitation.ExpiresAtUtc,

            UsedAtUtc = invitation.UsedAtUtc,

            RevokedAtUtc = invitation.RevokedAtUtc,

            LastSentAtUtc = invitation.LastSentAtUtc,

            DeliveryStatus = (InvitationDeliveryStatus)invitation.DeliveryStatus,

            DeliveryFailureReason = invitation.DeliveryFailureReason,

            CanResend = invitation.UsedAtUtc is null

                && invitation.RevokedAtUtc is null

                && cooldownRemaining == 0,

            ResendCooldownSecondsRemaining = cooldownRemaining,

        };

    }



    private static string GenerateToken()

    {

        Span<byte> bytes = stackalloc byte[TokenByteLength];

        RandomNumberGenerator.Fill(bytes);

        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    }



    private static string HashToken(string token)

    {

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));

        return Convert.ToHexString(hash);

    }



    private static string GenerateUnusablePassword()

    {

        Span<byte> bytes = stackalloc byte[48];

        RandomNumberGenerator.Fill(bytes);

        return Convert.ToBase64String(bytes);

    }

}


