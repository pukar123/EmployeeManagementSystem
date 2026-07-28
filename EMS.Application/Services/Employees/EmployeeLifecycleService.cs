using EMS.Application.DTOs.Employee;
using EMS.Application.Mapping;
using EMS.Application.Services.Authorization;
using EMS.Application.Services.Integrations;
using EMS.Domain.DbModels;
using EMS.Domain.Enums;
using EMS.Domain.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using Pukar.Shared;

namespace EMS.Application.Services.Employees;

public sealed class EmployeeLifecycleService : IEmployeeLifecycleService
{
    private readonly IBaseRepository<Employee> _employees;
    private readonly IBaseRepository<EmployeeEmploymentStatusHistory> _statusHistory;
    private readonly IBaseRepository<EmployeeRetentionPolicy> _retentionPolicyRepository;
    private readonly IIdentityContext _identityContext;
    private readonly IEmployeeUserManagementGateway _gateway;
    private readonly EmployeeRelationshipValidator _validator;
    private readonly IEmployeeBusinessDateHelper _businessDates;
    private readonly IIntegrationOutboxWriter _outboxWriter;

    public EmployeeLifecycleService(
        IBaseRepository<Employee> employees,
        IBaseRepository<EmployeeEmploymentStatusHistory> statusHistory,
        IBaseRepository<EmployeeRetentionPolicy> retentionPolicyRepository,
        IIdentityContext identityContext,
        IEmployeeUserManagementGateway gateway,
        EmployeeRelationshipValidator validator,
        IEmployeeBusinessDateHelper businessDates,
        IIntegrationOutboxWriter outboxWriter)
    {
        _employees = employees;
        _statusHistory = statusHistory;
        _retentionPolicyRepository = retentionPolicyRepository;
        _identityContext = identityContext;
        _gateway = gateway;
        _validator = validator;
        _businessDates = businessDates;
        _outboxWriter = outboxWriter;
    }

    public async Task<EmployeeResponseModel?> TerminateAsync(
        int id,
        TerminateEmployeeRequestModel request,
        CancellationToken cancellationToken = default)
    {
        var entity = await _employees.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return null;

        _validator.EnsureNotArchived(entity);

        if (entity.EmploymentStatus == EmploymentStatus.Terminated)
            throw new BusinessRuleException("Employee is already terminated.");

        var reason = request.Reason?.Trim();
        if (string.IsNullOrEmpty(reason))
            throw new BusinessRuleException("Termination reason is required.");
        reason = StringHelper.NormalizeRequired(reason);
        var effectiveDate = request.EffectiveDateUtc.ToUniversalTime();
        _businessDates.EnsureNotFutureForImmediateAction(effectiveDate, "termination");

        var previous = entity.EmploymentStatus;
        entity.EmploymentStatus = EmploymentStatus.Terminated;
        entity.IsActive = false;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        await ApplyRetentionFromPolicyAsync(entity, DateTime.UtcNow, cancellationToken);
        await AddStatusHistoryAsync(entity, previous, EmploymentStatus.Terminated, effectiveDate, reason, cancellationToken);
        await _outboxWriter.EnqueueRevokeLinkedIdentityAsync(entity.Id, cancellationToken);

        _employees.Update(entity);
        await _employees.SaveChangesAsync(cancellationToken);
        return EmployeeMapper.ToResponse(entity);
    }

    public async Task<EmployeeResponseModel?> ArchiveAsync(
        int id,
        ArchiveEmployeeRequestModel request,
        CancellationToken cancellationToken = default)
    {
        var entity = await _employees.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return null;

        if (entity.IsArchived)
            return EmployeeMapper.ToResponse(entity);

        var reason = request.Reason?.Trim();
        if (string.IsNullOrEmpty(reason))
            throw new BusinessRuleException("Archive reason is required.");
        reason = StringHelper.NormalizeRequired(reason);
        var now = DateTime.UtcNow;

        entity.IsArchived = true;
        entity.ArchivedAtUtc = now;
        entity.ArchiveReason = reason;
        entity.UpdatedAtUtc = now;

        await ApplyRetentionFromPolicyAsync(entity, now, cancellationToken);
        await _outboxWriter.EnqueueRevokeLinkedIdentityAsync(entity.Id, cancellationToken);

        _employees.Update(entity);
        await _employees.SaveChangesAsync(cancellationToken);
        return EmployeeMapper.ToResponse(entity);
    }

    public async Task<EmployeeResponseModel?> RestoreAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _employees.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return null;

        if (!entity.IsArchived)
            return EmployeeMapper.ToResponse(entity);

        if (entity.RetentionUntilUtc.HasValue && entity.RetentionUntilUtc.Value < DateTime.UtcNow)
        {
            throw new BusinessRuleException(
                "This employee record can no longer be restored because the retention period has ended.");
        }

        entity.IsArchived = false;
        entity.ArchivedAtUtc = null;
        entity.ArchiveReason = null;
        entity.RetentionUntilUtc = null;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        _employees.Update(entity);
        await _employees.SaveChangesAsync(cancellationToken);
        return EmployeeMapper.ToResponse(entity);
    }

    public async Task<EmployeeResponseModel?> ChangeEmploymentStatusAsync(
        int id,
        ChangeEmploymentStatusRequestModel request,
        CancellationToken cancellationToken = default)
    {
        var entity = await _employees.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return null;

        _validator.EnsureNotArchived(entity);

        if (request.NewStatus == EmploymentStatus.Terminated)
            throw new BusinessRuleException("Use the terminate action to end employment.");

        if (!Enum.IsDefined(request.NewStatus))
            throw new BusinessRuleException("Invalid employment status.");

        EnsureTransitionAllowed(entity.EmploymentStatus, request.NewStatus);

        var previous = entity.EmploymentStatus;
        if (previous == request.NewStatus)
            throw new BusinessRuleException("Employee already has this employment status.");

        var effectiveDate = request.EffectiveDateUtc.ToUniversalTime();
        _businessDates.EnsureNotFutureForImmediateAction(effectiveDate, "employment status change");
        var reason = StringHelper.NormalizeOptional(request.Reason);

        entity.EmploymentStatus = request.NewStatus;
        entity.IsActive = request.NewStatus == EmploymentStatus.Active;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        await AddStatusHistoryAsync(entity, previous, request.NewStatus, effectiveDate, reason, cancellationToken);

        _employees.Update(entity);
        await _employees.SaveChangesAsync(cancellationToken);
        return EmployeeMapper.ToResponse(entity);
    }

    private static void EnsureTransitionAllowed(EmploymentStatus current, EmploymentStatus next)
    {
        var allowed = (current, next) switch
        {
            (EmploymentStatus.Preboarding, EmploymentStatus.Active) => true,
            (EmploymentStatus.Preboarding, EmploymentStatus.Inactive) => true,
            (EmploymentStatus.Active, EmploymentStatus.Inactive) => true,
            (EmploymentStatus.Inactive, EmploymentStatus.Active) => true,
            (EmploymentStatus.Preboarding, EmploymentStatus.Preboarding) => false,
            (EmploymentStatus.Terminated, _) => false,
            (_, EmploymentStatus.Preboarding) => false,
            _ when next == EmploymentStatus.Terminated => false,
            _ => current != next,
        };

        if (!allowed)
        {
            throw new BusinessRuleException(
                $"Cannot change employment status from {current} to {next}.");
        }
    }

    private async Task AddStatusHistoryAsync(
        Employee entity,
        EmploymentStatus? previous,
        EmploymentStatus next,
        DateTime effectiveDate,
        string? reason,
        CancellationToken cancellationToken)
    {
        var actor = _identityContext.GetCurrent();
        var now = DateTime.UtcNow;

        await _statusHistory.AddAsync(
            new EmployeeEmploymentStatusHistory
            {
                Employee = entity,
                PreviousStatus = previous,
                NewStatus = next,
                EffectiveDateUtc = effectiveDate,
                Reason = reason,
                ChangedByUserId = actor.UserId,
                ChangedByUserName = actor.UserName,
                ChangedByEmail = actor.Email,
                CreatedAtUtc = now,
            },
            cancellationToken);
    }

    private async Task ApplyRetentionFromPolicyAsync(Employee entity, DateTime now, CancellationToken cancellationToken)
    {
        var retentionDays = await _retentionPolicyRepository.GetQueryable()
            .Where(x => x.IsEnabled && (x.OrganizationId == entity.OrganizationId || x.OrganizationId == null))
            .OrderByDescending(x => x.OrganizationId.HasValue)
            .ThenByDescending(x => x.Id)
            .Select(x => x.RetentionDays)
            .FirstOrDefaultAsync(cancellationToken);

        if (retentionDays <= 0)
            retentionDays = 3650;

        entity.RetentionUntilUtc = now.AddDays(retentionDays);
    }
}
