using EMS.Application.DTOs.Leave;
using EMS.Application.Mapping;
using EMS.Domain.DbModels;
using EMS.Domain.Enums;
using EMS.Domain.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using Pukar.Shared;

namespace EMS.Application.Services.Leave;

public sealed class LeaveRequestService : ILeaveRequestService
{
    private readonly ILeaveRequestRepository _leaveRequestRepository;
    private readonly ILeaveBalanceRepository _leaveBalanceRepository;
    private readonly ILeaveTypeRepository _leaveTypeRepository;
    private readonly IBaseRepository<Employee> _employeeRepository;

    public LeaveRequestService(
        ILeaveRequestRepository leaveRequestRepository,
        ILeaveBalanceRepository leaveBalanceRepository,
        ILeaveTypeRepository leaveTypeRepository,
        IBaseRepository<Employee> employeeRepository)
    {
        _leaveRequestRepository = leaveRequestRepository;
        _leaveBalanceRepository = leaveBalanceRepository;
        _leaveTypeRepository = leaveTypeRepository;
        _employeeRepository = employeeRepository;
    }

    public async Task<IReadOnlyList<LeaveRequestResponseModel>> GetByEmployeeAsync(
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        var rows = await _leaveRequestRepository.GetByEmployeeAsync(employeeId, cancellationToken);

        return rows.Select(LeaveMapper.ToResponse).ToList();
    }

    public async Task<LeaveRequestResponseModel> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _leaveRequestRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new BusinessRuleException("Leave request was not found.");

        return LeaveMapper.ToResponse(entity);
    }

    public async Task<LeaveRequestResponseModel> CreateAsync(
        CreateLeaveRequestRequestModel request,
        CancellationToken cancellationToken = default)
    {
        await ValidateLeaveRequestRangeAsync(
            request.EmployeeId,
            request.LeaveTypeId,
            request.StartDateUtc,
            request.EndDateUtc,
            null,
            request.RequestedAmount,
            cancellationToken);

        var employee = await _employeeRepository.GetByIdAsync(request.EmployeeId, cancellationToken)
            ?? throw new BusinessRuleException("Employee was not found.");

        var entity = LeaveMapper.CreateRequestEntity(request, employee.OrganizationId);

        await _leaveRequestRepository.AddAsync(entity, cancellationToken);
        await _leaveRequestRepository.SaveChangesAsync(cancellationToken);
        return LeaveMapper.ToResponse(entity);
    }

    public async Task<LeaveRequestResponseModel> UpdateAsync(
        int id,
        UpdateLeaveRequestRequestModel request,
        CancellationToken cancellationToken = default)
    {
        var entity = await _leaveRequestRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new BusinessRuleException("Leave request was not found.");

        if (entity.Status is not LeaveRequestStatus.Pending and not LeaveRequestStatus.ModifiedPending)
            throw new BusinessRuleException("Only pending leave requests can be modified.");

        await ValidateLeaveRequestRangeAsync(
            entity.EmployeeId,
            entity.LeaveTypeId,
            request.StartDateUtc,
            request.EndDateUtc,
            entity.Id,
            request.RequestedAmount,
            cancellationToken);

        entity.StartDateUtc = request.StartDateUtc.Date;
        entity.EndDateUtc = request.EndDateUtc.Date;
        entity.Unit = request.Unit;
        entity.RequestedAmount = request.RequestedAmount;
        entity.Reason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim();
        entity.Status = LeaveRequestStatus.ModifiedPending;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        _leaveRequestRepository.Update(entity);
        await _leaveRequestRepository.SaveChangesAsync(cancellationToken);
        return LeaveMapper.ToResponse(entity);
    }

    public async Task<LeaveRequestResponseModel> CancelAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _leaveRequestRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new BusinessRuleException("Leave request was not found.");

        if (entity.Status is LeaveRequestStatus.Cancelled or LeaveRequestStatus.Rejected)
            throw new BusinessRuleException("Leave request cannot be cancelled in its current state.");

        entity.Status = LeaveRequestStatus.Cancelled;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        _leaveRequestRepository.Update(entity);
        await _leaveRequestRepository.SaveChangesAsync(cancellationToken);
        return LeaveMapper.ToResponse(entity);
    }

    private async Task ValidateLeaveRequestRangeAsync(
        int employeeId,
        int leaveTypeId,
        DateTime startDateUtc,
        DateTime endDateUtc,
        int? currentRequestId,
        decimal requestedAmount,
        CancellationToken cancellationToken)
    {
        if (endDateUtc.Date < startDateUtc.Date)
            throw new BusinessRuleException("End date cannot be earlier than start date.");

        if (requestedAmount <= 0)
            throw new BusinessRuleException("Requested amount must be greater than zero.");

        var leaveType = await _leaveTypeRepository.GetByIdAsync(leaveTypeId, cancellationToken)
            ?? throw new BusinessRuleException("Leave type was not found.");
        if (!leaveType.IsActive)
            throw new BusinessRuleException("Selected leave type is not active.");

        var employee = await _employeeRepository.GetByIdAsync(employeeId, cancellationToken)
            ?? throw new BusinessRuleException("Employee was not found.");
        if (employee.OrganizationId != leaveType.OrganizationId)
            throw new BusinessRuleException("Employee and leave type must belong to the same organization.");

        var balance = await _leaveBalanceRepository.GetByEmployeeAndTypeAsync(
            employeeId,
            leaveTypeId,
            cancellationToken);
        if (balance is null)
            throw new BusinessRuleException("Leave balance was not found for the selected leave type.");

        var normalizedStart = startDateUtc.Date;
        var normalizedEnd = endDateUtc.Date;
        var overlappingExists = await _leaveRequestRepository.GetQueryable().AnyAsync(
            x =>
                x.EmployeeId == employeeId &&
                x.LeaveTypeId == leaveTypeId &&
                x.Status != LeaveRequestStatus.Cancelled &&
                x.Status != LeaveRequestStatus.Rejected &&
                (!currentRequestId.HasValue || x.Id != currentRequestId.Value) &&
                x.StartDateUtc <= normalizedEnd &&
                x.EndDateUtc >= normalizedStart,
            cancellationToken);
        if (overlappingExists)
            throw new BusinessRuleException("Requested date range overlaps an existing leave request.");

        var available = balance.OpeningBalance
            + balance.AccruedAmount
            + balance.AdjustedAmount
            + balance.CarryForwardAmount
            - balance.UsedAmount;

        if (requestedAmount > available)
            throw new BusinessRuleException("Insufficient leave balance for this request.");
    }
}
