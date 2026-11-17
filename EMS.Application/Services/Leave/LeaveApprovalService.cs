using EMS.Application.DTOs.Leave;
using EMS.Application.DTOs.Manager;
using EMS.Application.Mapping;
using EMS.Application.Services.Manager;
using EMS.Application.Services.Notifications;
using EMS.Domain.DbModels;
using EMS.Domain.Enums;
using EMS.Domain.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using Pukar.Shared;

namespace EMS.Application.Services.Leave;

public sealed class LeaveApprovalService : ILeaveApprovalService
{
    private readonly ILeaveRequestRepository _leaveRequestRepository;
    private readonly ILeaveBalanceRepository _leaveBalanceRepository;
    private readonly ILeaveTypeRepository _leaveTypeRepository;
    private readonly IBaseRepository<Employee> _employeeRepository;
    private readonly ILeaveApprovalAccessService _approvalAccess;
    private readonly IManagerTeamAccessService _managerTeamAccess;
    private readonly IEmsNotificationProducer _notificationProducer;

    public LeaveApprovalService(
        ILeaveRequestRepository leaveRequestRepository,
        ILeaveBalanceRepository leaveBalanceRepository,
        ILeaveTypeRepository leaveTypeRepository,
        IBaseRepository<Employee> employeeRepository,
        ILeaveApprovalAccessService approvalAccess,
        IManagerTeamAccessService managerTeamAccess,
        IEmsNotificationProducer notificationProducer)
    {
        _leaveRequestRepository = leaveRequestRepository;
        _leaveBalanceRepository = leaveBalanceRepository;
        _leaveTypeRepository = leaveTypeRepository;
        _employeeRepository = employeeRepository;
        _approvalAccess = approvalAccess;
        _managerTeamAccess = managerTeamAccess;
        _notificationProducer = notificationProducer;
    }

    public async Task<IReadOnlyList<ManagerLeaveApprovalItemResponseModel>> GetManagerQueueAsync(
        ManagerLeaveApprovalQueryModel query,
        CancellationToken cancellationToken = default)
    {
        // Same rule as the team dashboard: managers see their own team; admins may pass managerId.
        var managerId = await _managerTeamAccess.ResolveEffectiveManagerIdAsync(
            query.OrganizationId,
            query.ManagerId,
            cancellationToken);

        var team = await _employeeRepository.GetQueryable()
            .AsNoTracking()
            .Where(e => e.OrganizationId == query.OrganizationId && !e.IsArchived && e.ManagerId == managerId)
            .Select(e => new { e.Id, e.EmployeeNumber, e.FirstName, e.LastName })
            .ToListAsync(cancellationToken);
        if (team.Count == 0)
            return [];

        var teamIds = team.Select(e => e.Id).ToList();

        var requests = await _leaveRequestRepository.GetQueryable()
            .AsNoTracking()
            .Where(x => x.OrganizationId == query.OrganizationId
                        && teamIds.Contains(x.EmployeeId)
                        && (x.Status == LeaveRequestStatus.Pending || x.Status == LeaveRequestStatus.ModifiedPending))
            .OrderBy(x => x.StartDateUtc)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);
        if (requests.Count == 0)
            return [];

        var leaveTypeIds = requests.Select(x => x.LeaveTypeId).Distinct().ToList();
        var leaveTypeNames = await _leaveTypeRepository.GetQueryable()
            .AsNoTracking()
            .Where(t => leaveTypeIds.Contains(t.Id))
            .Select(t => new { t.Id, t.Name })
            .ToDictionaryAsync(t => t.Id, t => t.Name, cancellationToken);

        var requestEmployeeIds = requests.Select(x => x.EmployeeId).Distinct().ToList();
        var balances = await _leaveBalanceRepository.GetQueryable()
            .AsNoTracking()
            .Where(b => requestEmployeeIds.Contains(b.EmployeeId) && leaveTypeIds.Contains(b.LeaveTypeId))
            .ToListAsync(cancellationToken);
        var balanceLookup = balances
            .GroupBy(b => (b.EmployeeId, b.LeaveTypeId))
            .ToDictionary(g => g.Key, g => g.First());

        var employeesById = team.ToDictionary(e => e.Id);

        return requests
            .Select(x =>
            {
                var employee = employeesById[x.EmployeeId];
                balanceLookup.TryGetValue((x.EmployeeId, x.LeaveTypeId), out var balance);

                return new ManagerLeaveApprovalItemResponseModel
                {
                    LeaveRequestId = x.Id,
                    EmployeeId = x.EmployeeId,
                    EmployeeNumber = employee.EmployeeNumber,
                    EmployeeName = $"{employee.FirstName} {employee.LastName}".Trim(),
                    LeaveTypeId = x.LeaveTypeId,
                    LeaveTypeName = leaveTypeNames.GetValueOrDefault(x.LeaveTypeId) ?? string.Empty,
                    StartDateUtc = x.StartDateUtc,
                    EndDateUtc = x.EndDateUtc,
                    Unit = x.Unit,
                    RequestedAmount = x.RequestedAmount,
                    Reason = x.Reason,
                    Status = x.Status,
                    SubmittedAtUtc = x.SubmittedAtUtc,
                    AvailableBalance = balance is null ? null : decimal.Round(GetAvailable(balance), 2),
                };
            })
            .ToList();
    }

    public async Task<LeaveRequestResponseModel> ApproveAsync(
        int id,
        ApproveLeaveRequestRequestModel request,
        CancellationToken cancellationToken = default)
    {
        var entity = await LoadAsync(id, cancellationToken);
        var reviewerId = await _approvalAccess.EnsureCanDecideAsync(entity, cancellationToken);
        EnsureDecidable(entity);

        var comment = NormaliseComment(request.Comment);

        var balance = await _leaveBalanceRepository.GetByEmployeeAndTypeAsync(
            entity.EmployeeId,
            entity.LeaveTypeId,
            cancellationToken)
            ?? throw new BusinessRuleException(LeaveApprovalMessages.BalanceMissing);

        // Re-check at approval time: submissions do not reserve balance, so another approval may have used it.
        if (entity.RequestedAmount > GetAvailable(balance))
            throw new BusinessRuleException(LeaveApprovalMessages.InsufficientBalance);

        var now = DateTime.UtcNow;
        ApplyDecision(entity, LeaveRequestStatus.Approved, reviewerId, comment, now);

        balance.UsedAmount += entity.RequestedAmount;
        balance.UpdatedAtUtc = now;

        _leaveRequestRepository.Update(entity);
        _leaveBalanceRepository.Update(balance);
        // One SaveChanges = one transaction for the status change and the balance deduction.
        await SaveDecisionAsync(cancellationToken);

        await _notificationProducer.NotifyLeaveApprovedAsync(entity, cancellationToken);

        return LeaveMapper.ToResponse(entity);
    }

    public async Task<LeaveRequestResponseModel> RejectAsync(
        int id,
        RejectLeaveRequestRequestModel request,
        CancellationToken cancellationToken = default)
    {
        var entity = await LoadAsync(id, cancellationToken);
        var reviewerId = await _approvalAccess.EnsureCanDecideAsync(entity, cancellationToken);
        EnsureDecidable(entity);

        var reason = NormaliseComment(request.Reason)
            ?? throw new BusinessRuleException(LeaveApprovalMessages.RejectionReasonRequired);

        ApplyDecision(entity, LeaveRequestStatus.Rejected, reviewerId, reason, DateTime.UtcNow);

        _leaveRequestRepository.Update(entity);
        await SaveDecisionAsync(cancellationToken);

        await _notificationProducer.NotifyLeaveRejectedAsync(entity, cancellationToken);

        return LeaveMapper.ToResponse(entity);
    }

    private async Task<LeaveRequest> LoadAsync(int id, CancellationToken cancellationToken)
        => await _leaveRequestRepository.GetByIdAsync(id, cancellationToken)
           ?? throw new BusinessRuleException("Leave request was not found.");

    private static void EnsureDecidable(LeaveRequest entity)
    {
        if (entity.Status is not LeaveRequestStatus.Pending and not LeaveRequestStatus.ModifiedPending)
            throw new BusinessRuleException(LeaveApprovalMessages.NotDecidable);
    }

    private static string? NormaliseComment(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        if (trimmed.Length > LeaveApprovalMessages.MaxCommentLength)
            throw new BusinessRuleException(LeaveApprovalMessages.CommentTooLong);

        return trimmed;
    }

    private static void ApplyDecision(
        LeaveRequest entity,
        LeaveRequestStatus status,
        int reviewerEmployeeId,
        string? comment,
        DateTime nowUtc)
    {
        entity.Status = status;
        entity.ReviewedByEmployeeId = reviewerEmployeeId;
        entity.ReviewedAtUtc = nowUtc;
        entity.DecisionComment = comment;
        entity.UpdatedAtUtc = nowUtc;
    }

    private async Task SaveDecisionAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _leaveRequestRepository.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // RowVersion mismatch: someone else decided, edited or cancelled first. Nothing was saved.
            throw new BusinessRuleException(LeaveApprovalMessages.ConcurrencyConflict);
        }
    }

    private static decimal GetAvailable(LeaveBalance balance)
        => balance.OpeningBalance
           + balance.AccruedAmount
           + balance.AdjustedAmount
           + balance.CarryForwardAmount
           - balance.UsedAmount;
}
