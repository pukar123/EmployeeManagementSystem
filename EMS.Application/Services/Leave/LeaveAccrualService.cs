using EMS.Application.DTOs.Leave;
using EMS.Domain.DbModels;
using EMS.Domain.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using Pukar.Shared;

namespace EMS.Application.Services.Leave;

public sealed class LeaveAccrualService : ILeaveAccrualService
{
    private readonly ILeavePolicyRuleRepository _policyRepository;
    private readonly ILeaveBalanceRepository _balanceRepository;
    private readonly IBaseRepository<Employee> _employeeRepository;

    public LeaveAccrualService(
        ILeavePolicyRuleRepository policyRepository,
        ILeaveBalanceRepository balanceRepository,
        IBaseRepository<Employee> employeeRepository)
    {
        _policyRepository = policyRepository;
        _balanceRepository = balanceRepository;
        _employeeRepository = employeeRepository;
    }

    public async Task<LeaveAccrualRunResultResponseModel> RunAccrualAsync(
        RunLeaveAccrualRequestModel request,
        CancellationToken cancellationToken = default)
    {
        var asOfDate = request.AsOfUtc.Date;
        var employees = await _employeeRepository.GetQueryable()
            .Where(x => x.OrganizationId == request.OrganizationId && !x.IsArchived && x.IsActive)
            .ToListAsync(cancellationToken);
        var policies = await _policyRepository.GetQueryable()
            .Where(x =>
                x.OrganizationId == request.OrganizationId &&
                x.IsActive &&
                x.EffectiveFromDateUtc <= asOfDate &&
                (!x.EffectiveToDateUtc.HasValue || x.EffectiveToDateUtc.Value >= asOfDate))
            .ToListAsync(cancellationToken);

        if (policies.Count == 0)
            throw new BusinessRuleException("No active leave policy rules found for accrual run.");

        var created = 0;
        var updated = 0;

        foreach (var employee in employees)
        {
            foreach (var policy in policies)
            {
                var balance = await _balanceRepository.GetByEmployeeAndTypeAsync(
                    employee.Id,
                    policy.LeaveTypeId,
                    cancellationToken);
                if (balance is null)
                {
                    balance = new LeaveBalance
                    {
                        OrganizationId = request.OrganizationId,
                        EmployeeId = employee.Id,
                        LeaveTypeId = policy.LeaveTypeId,
                        OpeningBalance = 0,
                        AccruedAmount = 0,
                        UsedAmount = 0,
                        AdjustedAmount = 0,
                        CarryForwardAmount = 0,
                        BalanceAsOfUtc = asOfDate,
                        CreatedAtUtc = DateTime.UtcNow,
                        UpdatedAtUtc = DateTime.UtcNow,
                    };
                    await _balanceRepository.AddAsync(balance, cancellationToken);
                    created++;
                }

                var accrual = policy.AccrualRatePerPeriod;
                if (policy.EnableProration)
                {
                    var daysInMonth = DateTime.DaysInMonth(asOfDate.Year, asOfDate.Month);
                    var workedDays = Math.Clamp((asOfDate - employee.DateJoined.Date).Days + 1, 0, daysInMonth);
                    if (workedDays < daysInMonth)
                        accrual = decimal.Round(accrual * workedDays / daysInMonth, 2);
                }

                balance.AccruedAmount += accrual;
                balance.BalanceAsOfUtc = asOfDate;
                balance.UpdatedAtUtc = DateTime.UtcNow;
                _balanceRepository.Update(balance);
                updated++;
            }
        }

        await _balanceRepository.SaveChangesAsync(cancellationToken);

        return new LeaveAccrualRunResultResponseModel
        {
            OrganizationId = request.OrganizationId,
            AsOfUtc = asOfDate,
            EmployeesProcessed = employees.Count,
            BalancesCreated = created,
            BalancesUpdated = updated,
        };
    }

    public async Task<LeaveYearResetResultResponseModel> RunLeaveYearResetAsync(
        RunLeaveYearResetRequestModel request,
        CancellationToken cancellationToken = default)
    {
        var startDate = request.LeaveYearStartDateUtc.Date;
        var policies = await _policyRepository.GetQueryable()
            .Where(x => x.OrganizationId == request.OrganizationId && x.IsActive)
            .ToListAsync(cancellationToken);

        var policyByLeaveType = policies
            .GroupBy(x => x.LeaveTypeId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.EffectiveFromDateUtc).First());

        var balances = await _balanceRepository.GetQueryable()
            .Where(x => x.OrganizationId == request.OrganizationId)
            .ToListAsync(cancellationToken);

        foreach (var balance in balances)
        {
            var available = balance.OpeningBalance
                + balance.AccruedAmount
                + balance.AdjustedAmount
                + balance.CarryForwardAmount
                - balance.UsedAmount;

            var carryForward = available > 0 ? available : 0;
            if (policyByLeaveType.TryGetValue(balance.LeaveTypeId, out var policy) &&
                policy.MaximumCarryForward.HasValue)
            {
                carryForward = Math.Min(carryForward, policy.MaximumCarryForward.Value);
            }

            balance.OpeningBalance = carryForward;
            balance.AccruedAmount = 0;
            balance.UsedAmount = 0;
            balance.AdjustedAmount = 0;
            balance.CarryForwardAmount = carryForward;
            balance.BalanceAsOfUtc = startDate;
            balance.UpdatedAtUtc = DateTime.UtcNow;
            _balanceRepository.Update(balance);
        }

        await _balanceRepository.SaveChangesAsync(cancellationToken);

        return new LeaveYearResetResultResponseModel
        {
            OrganizationId = request.OrganizationId,
            LeaveYearStartDateUtc = startDate,
            BalancesReset = balances.Count,
        };
    }
}
