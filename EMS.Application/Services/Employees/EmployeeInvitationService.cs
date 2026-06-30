using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using EMS.Application.DTOs.Employee;
using EMS.Application.Options;
using EMS.Application.Services.Email;
using EMS.Domain.DbModels;
using EMS.Domain.Enums;
using EMS.Domain.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Pukar.Shared;

namespace EMS.Application.Services.Employees;

public sealed class EmployeeInvitationService : IEmployeeInvitationService
{
    private const int ResendCooldownSeconds = 60;
    private const int InvitationLifetimeHours = 24;
    private const int TokenByteLength = 32;

    private readonly IBaseRepository<Employee> _employees;
    private readonly IBaseRepository<EmployeeInvitation> _invitations;
    private readonly IEmployeeUserManagementGateway _gateway;
    private readonly IEmailSender _emailSender;
    private readonly SmtpOptions _smtpOptions;
    private readonly EmployeeRelationshipValidator _validator;

    public EmployeeInvitationService(
        IBaseRepository<Employee> employees,
        IBaseRepository<EmployeeInvitation> invitations,
        IEmployeeUserManagementGateway gateway,
        IEmailSender emailSender,
        IOptions<SmtpOptions> smtpOptions,
        EmployeeRelationshipValidator validator)
    {
        _employees = employees;
        _invitations = invitations;
        _gateway = gateway;
        _emailSender = emailSender;
        _smtpOptions = smtpOptions.Value;
        _validator = validator;
    }

    public async Task<EmployeeInvitationResponseModel> SendAsync(int employeeId, CancellationToken cancellationToken = default)
    {
        var employee = await _employees.GetByIdAsync(employeeId, cancellationToken)
            ?? throw new BusinessRuleException("Employee was not found.");

        _validator.EnsureNotArchived(employee);

        var activeInvitation = await _invitations.GetQueryable()
            .Where(i => i.EmployeeId == employeeId && i.UsedAtUtc == null && i.RevokedAtUtc == null)
            .OrderByDescending(i => i.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (activeInvitation is not null)
        {
            if (activeInvitation.LastSentAtUtc.HasValue
                && DateTime.UtcNow - activeInvitation.LastSentAtUtc.Value < TimeSpan.FromSeconds(ResendCooldownSeconds))
            {
                throw new BusinessRuleException("Please wait before resending the invitation.");
            }

            return await ResendExistingAsync(employee, activeInvitation, cancellationToken);
        }

        return await CreateAndSendAsync(employee, cancellationToken);
    }

    public async Task<IReadOnlyList<EmployeeInvitationResponseModel>> ListAsync(
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        var items = await _invitations.GetQueryable()
            .AsNoTracking()
            .Where(i => i.EmployeeId == employeeId)
            .OrderByDescending(i => i.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return items.Select(ToResponse).ToList();
    }

    public async Task RevokeAsync(int employeeId, int invitationId, CancellationToken cancellationToken = default)
    {
        var invitation = await _invitations.GetQueryable()
            .FirstOrDefaultAsync(i => i.Id == invitationId && i.EmployeeId == employeeId, cancellationToken)
            ?? throw new BusinessRuleException("Invitation was not found.");

        if (invitation.UsedAtUtc.HasValue)
            throw new BusinessRuleException("Used invitations cannot be revoked.");

        invitation.RevokedAtUtc = DateTime.UtcNow;
        _invitations.Update(invitation);
        await _invitations.SaveChangesAsync(cancellationToken);
    }

    public async Task AcceptAsync(AcceptEmployeeInvitationRequestModel request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
            throw new BusinessRuleException("Invitation token is required.");

        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 8)
            throw new BusinessRuleException("Password must be at least 8 characters.");

        var tokenHash = HashToken(request.Token.Trim());
        var invitation = await _invitations.GetQueryable()
            .FirstOrDefaultAsync(i => i.TokenHash == tokenHash, cancellationToken)
            ?? throw new BusinessRuleException("Invitation is invalid or has expired.");

        if (invitation.UsedAtUtc.HasValue)
            throw new BusinessRuleException("Invitation has already been used.");

        if (invitation.RevokedAtUtc.HasValue)
            throw new BusinessRuleException("Invitation has been revoked.");

        if (invitation.ExpiresAtUtc < DateTime.UtcNow)
            throw new BusinessRuleException("Invitation has expired.");

        await _gateway.SetLinkedUserPasswordAsync(invitation.UserId, request.NewPassword, false, cancellationToken);
        await _gateway.ActivateLinkedUserAsync(invitation.UserId, cancellationToken);

        invitation.UsedAtUtc = DateTime.UtcNow;
        _invitations.Update(invitation);
        await _invitations.SaveChangesAsync(cancellationToken);
    }

    internal static async Task RevokePendingForEmployeeAsync(
        IBaseRepository<EmployeeInvitation> invitations,
        int employeeId,
        CancellationToken cancellationToken)
    {
        var pending = await invitations.GetQueryable()
            .Where(i => i.EmployeeId == employeeId && i.UsedAtUtc == null && i.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        foreach (var invitation in pending)
        {
            invitation.RevokedAtUtc = now;
            invitations.Update(invitation);
        }

        if (pending.Count > 0)
            await invitations.SaveChangesAsync(cancellationToken);
    }

    private async Task<EmployeeInvitationResponseModel> CreateAndSendAsync(Employee employee, CancellationToken cancellationToken)
    {
        var existingUser = await _gateway.GetUserByEmailAsync(employee.Email, cancellationToken);
        int userId;
        if (existingUser is not null)
        {
            userId = existingUser.Id;
            await _gateway.DeactivateLinkedUserAsync(userId, cancellationToken);
        }
        else
        {
            var created = await _gateway.CreateUserAsync(
                new CreateEmployeeLinkedUserRequest
                {
                    Email = employee.Email,
                    UserName = $"{employee.FirstName} {employee.LastName}".Trim(),
                    Password = GenerateUnusablePassword(),
                    IsActive = false,
                    MustChangePassword = false,
                },
                cancellationToken);
            userId = created.Id;
            employee.ExternalIdentityKey = userId.ToString(CultureInfo.InvariantCulture);
            _employees.Update(employee);
        }

        var rawToken = GenerateToken();
        var now = DateTime.UtcNow;
        var invitation = new EmployeeInvitation
        {
            EmployeeId = employee.Id,
            UserId = userId,
            TokenHash = HashToken(rawToken),
            ExpiresAtUtc = now.AddHours(InvitationLifetimeHours),
            DeliveryStatus = EmployeeInvitationDeliveryStatus.Pending,
            CreatedAtUtc = now,
        };

        await _invitations.AddAsync(invitation, cancellationToken);
        await _invitations.SaveChangesAsync(cancellationToken);

        try
        {
            await SendEmailAsync(employee, rawToken, cancellationToken);
            invitation.DeliveryStatus = EmployeeInvitationDeliveryStatus.Sent;
            invitation.LastSentAtUtc = DateTime.UtcNow;
            invitation.DeliveryFailureReason = null;
        }
        catch (Exception ex)
        {
            invitation.DeliveryStatus = EmployeeInvitationDeliveryStatus.Failed;
            invitation.DeliveryFailureReason = ex.Message.Length > 2000 ? ex.Message[..2000] : ex.Message;
            _invitations.Update(invitation);
            await _invitations.SaveChangesAsync(cancellationToken);
            throw new BusinessRuleException("Invitation email could not be delivered. The invitation remains pending for retry.");
        }

        _invitations.Update(invitation);
        await _invitations.SaveChangesAsync(cancellationToken);
        return ToResponse(invitation);
    }

    private async Task<EmployeeInvitationResponseModel> ResendExistingAsync(
        Employee employee,
        EmployeeInvitation invitation,
        CancellationToken cancellationToken)
    {
        var rawToken = GenerateToken();
        invitation.TokenHash = HashToken(rawToken);
        invitation.ExpiresAtUtc = DateTime.UtcNow.AddHours(InvitationLifetimeHours);
        invitation.DeliveryStatus = EmployeeInvitationDeliveryStatus.Pending;
        invitation.DeliveryFailureReason = null;

        try
        {
            await SendEmailAsync(employee, rawToken, cancellationToken);
            invitation.DeliveryStatus = EmployeeInvitationDeliveryStatus.Sent;
            invitation.LastSentAtUtc = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            invitation.DeliveryStatus = EmployeeInvitationDeliveryStatus.Failed;
            invitation.DeliveryFailureReason = ex.Message.Length > 2000 ? ex.Message[..2000] : ex.Message;
            _invitations.Update(invitation);
            await _invitations.SaveChangesAsync(cancellationToken);
            throw new BusinessRuleException("Invitation email could not be delivered.");
        }

        _invitations.Update(invitation);
        await _invitations.SaveChangesAsync(cancellationToken);
        return ToResponse(invitation);
    }

    private async Task SendEmailAsync(Employee employee, string rawToken, CancellationToken cancellationToken)
    {
        var baseUrl = _smtpOptions.WebAppBaseUrl.TrimEnd('/');
        var link = $"{baseUrl}/accept-invitation?token={Uri.EscapeDataString(rawToken)}";
        await _emailSender.SendAsync(
            new EmailMessage
            {
                ToEmail = employee.Email,
                Subject = "You're invited to Employee Management System",
                Body = $"""
                    <p>Hello {employee.FirstName},</p>
                    <p>You have been invited to access the Employee Management System.</p>
                    <p><a href="{link}">Accept invitation and set your password</a></p>
                    <p>This link expires in 24 hours and can only be used once.</p>
                    """,
            },
            cancellationToken);
    }

    private static EmployeeInvitationResponseModel ToResponse(EmployeeInvitation invitation)
    {
        var cooldownRemaining = 0;
        if (invitation.LastSentAtUtc.HasValue)
        {
            var elapsed = (int)(DateTime.UtcNow - invitation.LastSentAtUtc.Value).TotalSeconds;
            cooldownRemaining = Math.Max(0, ResendCooldownSeconds - elapsed);
        }

        return new EmployeeInvitationResponseModel
        {
            Id = invitation.Id,
            EmployeeId = invitation.EmployeeId,
            ExpiresAtUtc = invitation.ExpiresAtUtc,
            UsedAtUtc = invitation.UsedAtUtc,
            RevokedAtUtc = invitation.RevokedAtUtc,
            LastSentAtUtc = invitation.LastSentAtUtc,
            DeliveryStatus = invitation.DeliveryStatus,
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
