using System.Globalization;
using EMS.Application.DTOs.Employee;
using EMS.Application.Services.Employees;
using EMS.Application.Services.Integrations;
using EMS.Domain.DbModels;
using EMS.Domain.Enums;
using EMS.Domain.Repositories.Interface;
using Pukar.Shared;
using Pukar.Usermanagement.Contracts.Invitations;
using UmDeliveryStatus = Pukar.Usermanagement.Contracts.Invitations.InvitationDeliveryStatus;

namespace EMS.Infrastructure.Integrations.UserManagement;

public sealed class HttpEmployeeInvitationService : IEmployeeInvitationService
{
    private readonly IBaseRepository<Employee> _employees;
    private readonly IUserManagementHttpClient _http;
    private readonly EmployeeRelationshipValidator _validator;

    public HttpEmployeeInvitationService(
        IBaseRepository<Employee> employees,
        IUserManagementHttpClient http,
        EmployeeRelationshipValidator validator)
    {
        _employees = employees;
        _http = http;
        _validator = validator;
    }

    public async Task<EmployeeInvitationResponseModel> SendAsync(int employeeId, CancellationToken cancellationToken = default)
    {
        var employee = await LoadEligibleEmployeeAsync(employeeId, cancellationToken);

        int? linkUserId = int.TryParse(employee.ExternalIdentityKey, out var parsed) ? parsed : null;

        var invitation = await _http.PostAsync<InvitationResponseModel>(
            "/api/internal/v1/invitations",
            new CreateInvitationRequestModel
            {
                ExternalCorrelationId = CorrelationId(employeeId),
                RecipientEmail = employee.Email,
                RecipientDisplayName = BuildDisplayName(employee),
                LinkExistingInactiveUserId = linkUserId,
            },
            cancellationToken,
            allowRetry: false,
            idempotencyKey: $"invite:employee:{employeeId}")
            ?? throw new UserManagementDependencyUnavailableException("User Management returned an empty invitation response.");

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

        return Map(invitation, employeeId);
    }

    public async Task<IReadOnlyList<EmployeeInvitationResponseModel>> ListAsync(
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        _ = await LoadEligibleEmployeeAsync(employeeId, cancellationToken, requireActiveEmail: false);

        var invitations = await _http.GetAsync<List<InvitationResponseModel>>(
            $"/api/internal/v1/invitations?correlationId={Uri.EscapeDataString(CorrelationId(employeeId))}",
            cancellationToken,
            allowRetry: true);

        return (invitations ?? [])
            .Select(i => Map(i, employeeId))
            .ToList();
    }

    public async Task RevokeAsync(int employeeId, int invitationId, CancellationToken cancellationToken = default)
    {
        _ = await LoadEligibleEmployeeAsync(employeeId, cancellationToken, requireActiveEmail: false);

        await _http.DeleteAsync(
            $"/api/internal/v1/invitations/{invitationId}",
            cancellationToken,
            idempotencyKey: $"revoke-invitation:employee:{employeeId}:invitation:{invitationId}");
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
