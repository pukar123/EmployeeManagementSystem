using System.Globalization;
using EMS.Application.DTOs.Employee;
using EMS.Application.Services.Authorization;
using EMS.Application.Services.Employees;
using EMS.Application.Services.Notifications;
using EMS.Domain.DbModels;
using EMS.Domain.Enums;
using EMS.Domain.Repositories.Interface;
using Pukar.Shared;
using Pukar.Usermanagement.Application.Services.Invitations;
using Pukar.Usermanagement.Contracts.Invitations;
using UmDeliveryStatus = Pukar.Usermanagement.Contracts.Invitations.InvitationDeliveryStatus;

namespace EMS.Infrastructure.Integrations.UserManagement;

/// <summary>
/// In-process adapter fulfilling the EMS-owned <see cref="IEmployeeInvitationService"/> by
/// calling the User Management <see cref="IInvitationService"/> application service directly.
/// Replaces the previous HTTP + service-token integration with the remote UM host.
/// </summary>
public sealed class InProcessEmployeeInvitationService : IEmployeeInvitationService
{
    private readonly IBaseRepository<Employee> _employees;
    private readonly IInvitationService _invitations;
    private readonly EmployeeRelationshipValidator _validator;
    private readonly IIdentityContext _identityContext;
    private readonly IEmsNotificationProducer _notificationProducer;

    public InProcessEmployeeInvitationService(
        IBaseRepository<Employee> employees,
        IInvitationService invitations,
        EmployeeRelationshipValidator validator,
        IIdentityContext identityContext,
        IEmsNotificationProducer notificationProducer)
    {
        _employees = employees;
        _invitations = invitations;
        _validator = validator;
        _identityContext = identityContext;
        _notificationProducer = notificationProducer;
    }

    public async Task<EmployeeInvitationResponseModel> SendAsync(int employeeId, CancellationToken cancellationToken = default)
    {
        var employee = await LoadEligibleEmployeeAsync(employeeId, cancellationToken);

        int? linkUserId = int.TryParse(employee.ExternalIdentityKey, out var parsed) ? parsed : null;

        var invitation = await _invitations.CreateOrSendAsync(
            new CreateInvitationRequestModel
            {
                ExternalCorrelationId = CorrelationId(employeeId),
                RecipientEmail = employee.Email,
                RecipientDisplayName = BuildDisplayName(employee),
                LinkExistingInactiveUserId = linkUserId,
            },
            cancellationToken);

        var externalKey = invitation.UserId.ToString(CultureInfo.InvariantCulture);
        if (!string.Equals(employee.ExternalIdentityKey, externalKey, StringComparison.Ordinal))
        {
            await EmployeeLinkedIdentityHelper.EnsureNoOtherActiveEmployeeUsesExternalIdentityKeyAsync(
                _employees,
                employee.Id,
                externalKey,
                cancellationToken);

            employee.ExternalIdentityKey = externalKey;
            employee.UpdatedAtUtc = DateTime.UtcNow;
            _employees.Update(employee);
            await _employees.SaveChangesAsync(cancellationToken);
        }

        return await MapAndNotifyAsync(invitation, employeeId, cancellationToken);
    }

    public async Task<IReadOnlyList<EmployeeInvitationResponseModel>> ListAsync(
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        _ = await LoadEligibleEmployeeAsync(employeeId, cancellationToken, requireActiveEmail: false);

        var invitations = await _invitations.ListByCorrelationIdAsync(CorrelationId(employeeId), cancellationToken);
        return invitations.Select(i => Map(i, employeeId)).ToList();
    }

    public async Task RevokeAsync(int employeeId, int invitationId, CancellationToken cancellationToken = default)
    {
        _ = await LoadEligibleEmployeeAsync(employeeId, cancellationToken, requireActiveEmail: false);
        await _invitations.RevokeAsync(invitationId, cancellationToken);
    }

    private async Task<EmployeeInvitationResponseModel> MapAndNotifyAsync(
        InvitationResponseModel invitation,
        int employeeId,
        CancellationToken cancellationToken)
    {
        var mapped = Map(invitation, employeeId);
        await NotifyIfDeliveryFailedAsync(mapped, employeeId, cancellationToken);
        return mapped;
    }

    private async Task NotifyIfDeliveryFailedAsync(
        EmployeeInvitationResponseModel invitation,
        int employeeId,
        CancellationToken cancellationToken)
    {
        if (invitation.DeliveryStatus != EmployeeInvitationDeliveryStatus.Failed)
            return;

        var actorUserId = _identityContext.GetCurrent().UserId;
        if (actorUserId is null)
            return;

        await _notificationProducer.NotifyInvitationDeliveryFailedAsync(
            employeeId,
            actorUserId.Value,
            invitation.DeliveryFailureReason,
            cancellationToken);
    }

    private async Task<Employee> LoadEligibleEmployeeAsync(
        int employeeId,
        CancellationToken cancellationToken,
        bool requireActiveEmail = true)
    {
        var employee = await _employees.GetByIdAsync(employeeId, cancellationToken);
        if (employee is null)
            throw new BusinessRuleException("Employee was not found.");

        _validator.EnsureNotArchived(employee);

        if (requireActiveEmail && string.IsNullOrWhiteSpace(employee.Email))
            throw new BusinessRuleException("Employee email is required before sending an invitation.");

        return employee;
    }

    private static string CorrelationId(int employeeId)
        => $"employee:{employeeId.ToString(CultureInfo.InvariantCulture)}";

    private static string BuildDisplayName(Employee employee)
        => $"{employee.FirstName} {employee.LastName}".Trim();

    private static EmployeeInvitationResponseModel Map(InvitationResponseModel invitation, int employeeId)
        => new()
        {
            Id = invitation.Id,
            EmployeeId = employeeId,
            ExpiresAtUtc = invitation.ExpiresAtUtc,
            UsedAtUtc = invitation.UsedAtUtc,
            RevokedAtUtc = invitation.RevokedAtUtc,
            LastSentAtUtc = invitation.LastSentAtUtc,
            DeliveryStatus = MapDeliveryStatus(invitation.DeliveryStatus),
            DeliveryFailureReason = invitation.DeliveryFailureReason,
            CanResend = invitation.CanResend,
            ResendCooldownSecondsRemaining = invitation.ResendCooldownSecondsRemaining,
        };

    private static EmployeeInvitationDeliveryStatus MapDeliveryStatus(UmDeliveryStatus status)
        => status switch
        {
            UmDeliveryStatus.Sent => EmployeeInvitationDeliveryStatus.Sent,
            UmDeliveryStatus.Failed => EmployeeInvitationDeliveryStatus.Failed,
            _ => EmployeeInvitationDeliveryStatus.Pending,
        };
}
