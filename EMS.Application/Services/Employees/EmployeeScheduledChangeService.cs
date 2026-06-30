using EMS.Application.DTOs.Employee;
using EMS.Application.Services.Authorization;
using EMS.Domain.DbModels;
using EMS.Domain.Enums;
using EMS.Domain.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using Pukar.Shared;

namespace EMS.Application.Services.Employees;

public sealed class EmployeeScheduledChangeService : IEmployeeScheduledChangeService
{
    private readonly IBaseRepository<Employee> _employees;
    private readonly IBaseRepository<EmployeeScheduledChange> _scheduledChanges;
    private readonly IIdentityContext _identityContext;
    private readonly IEmployeeBusinessDateHelper _businessDates;
    private readonly EmployeeRelationshipValidator _validator;

    public EmployeeScheduledChangeService(
        IBaseRepository<Employee> employees,
        IBaseRepository<EmployeeScheduledChange> scheduledChanges,
        IIdentityContext identityContext,
        IEmployeeBusinessDateHelper businessDates,
        EmployeeRelationshipValidator validator)
    {
        _employees = employees;
        _scheduledChanges = scheduledChanges;
        _identityContext = identityContext;
        _businessDates = businessDates;
        _validator = validator;
    }

    public async Task<EmployeeScheduledChangeResponseModel> CreateAsync(
        int employeeId,
        CreateEmployeeScheduledChangeRequestModel request,
        CancellationToken cancellationToken = default)
    {
        var employee = await _employees.GetByIdAsync(employeeId, cancellationToken);
        if (employee is null)
            throw new BusinessRuleException("Employee was not found.");

        _validator.EnsureNotArchived(employee);

        var effectiveAt = request.EffectiveAtUtc.ToUniversalTime();
        _businessDates.EnsureFutureForScheduledAction(effectiveAt);

        ValidateRequest(employee, request);

        var hasPending = await _scheduledChanges.GetQueryable()
            .AnyAsync(
                c => c.EmployeeId == employeeId
                     && c.ChangeType == request.ChangeType
                     && c.Status == EmployeeScheduledChangeStatus.Pending,
                cancellationToken);
        if (hasPending)
            throw new BusinessRuleException("A pending scheduled change of this type already exists for this employee.");

        var actor = _identityContext.GetCurrent();
        var now = DateTime.UtcNow;
        var entity = new EmployeeScheduledChange
        {
            EmployeeId = employeeId,
            ChangeType = request.ChangeType,
            TargetReferenceId = request.TargetReferenceId,
            TargetStatus = request.TargetStatus.HasValue ? (int)request.TargetStatus.Value : null,
            EffectiveAtUtc = effectiveAt,
            Reason = StringHelper.NormalizeOptional(request.Reason),
            Status = EmployeeScheduledChangeStatus.Pending,
            CreatedByUserId = actor.UserId,
            CreatedByUserName = actor.UserName,
            CreatedByEmail = actor.Email,
            CreatedAtUtc = now,
        };

        await _scheduledChanges.AddAsync(entity, cancellationToken);
        await _scheduledChanges.SaveChangesAsync(cancellationToken);
        return ToResponse(entity);
    }

    public async Task<IReadOnlyList<EmployeeScheduledChangeResponseModel>> ListAsync(
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        var items = await _scheduledChanges.GetQueryable()
            .AsNoTracking()
            .Where(c => c.EmployeeId == employeeId)
            .OrderByDescending(c => c.EffectiveAtUtc)
            .ThenByDescending(c => c.Id)
            .ToListAsync(cancellationToken);

        return items.Select(ToResponse).ToList();
    }

    public async Task CancelAsync(int employeeId, int changeId, CancellationToken cancellationToken = default)
    {
        var change = await _scheduledChanges.GetQueryable()
            .FirstOrDefaultAsync(c => c.Id == changeId && c.EmployeeId == employeeId, cancellationToken);
        if (change is null)
            throw new BusinessRuleException("Scheduled change was not found.");

        if (change.Status != EmployeeScheduledChangeStatus.Pending)
            throw new BusinessRuleException("Only pending scheduled changes can be cancelled.");

        change.Status = EmployeeScheduledChangeStatus.Cancelled;
        change.ProcessedAtUtc = DateTime.UtcNow;
        _scheduledChanges.Update(change);
        await _scheduledChanges.SaveChangesAsync(cancellationToken);
    }

    private void ValidateRequest(Employee employee, CreateEmployeeScheduledChangeRequestModel request)
    {
        switch (request.ChangeType)
        {
            case EmployeeScheduledChangeType.Terminate:
                if (string.IsNullOrWhiteSpace(request.Reason))
                    throw new BusinessRuleException("Termination reason is required.");
                if (employee.EmploymentStatus == EmploymentStatus.Terminated)
                    throw new BusinessRuleException("Employee is already terminated.");
                break;
            case EmployeeScheduledChangeType.Archive:
                if (string.IsNullOrWhiteSpace(request.Reason))
                    throw new BusinessRuleException("Archive reason is required.");
                break;
            case EmployeeScheduledChangeType.ChangeEmploymentStatus:
                if (!request.TargetStatus.HasValue || !Enum.IsDefined(request.TargetStatus.Value))
                    throw new BusinessRuleException("Target employment status is required.");
                if (request.TargetStatus == EmploymentStatus.Terminated)
                    throw new BusinessRuleException("Use terminate for ending employment.");
                break;
            case EmployeeScheduledChangeType.TransferDepartment:
            case EmployeeScheduledChangeType.TransferPosition:
            case EmployeeScheduledChangeType.TransferManager:
                break;
            case EmployeeScheduledChangeType.RestoreRecord:
                throw new BusinessRuleException("Restore cannot be scheduled.");
            default:
                throw new BusinessRuleException("Unsupported scheduled change type.");
        }
    }

    internal static EmployeeScheduledChangeResponseModel ToResponse(EmployeeScheduledChange entity)
        => new()
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            ChangeType = entity.ChangeType,
            TargetReferenceId = entity.TargetReferenceId,
            TargetStatus = entity.TargetStatus.HasValue ? (EmploymentStatus)entity.TargetStatus.Value : null,
            EffectiveAtUtc = entity.EffectiveAtUtc,
            Reason = entity.Reason,
            Status = entity.Status,
            CreatedAtUtc = entity.CreatedAtUtc,
            ProcessedAtUtc = entity.ProcessedAtUtc,
            FailureReason = entity.FailureReason,
        };
}
