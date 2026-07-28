using EMS.Application.DTOs.Leave;
using EMS.Application.Mapping;
using EMS.Domain.DbModels;
using EMS.Domain.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using Pukar.Shared;

namespace EMS.Application.Services.Leave;

public sealed class LeaveBalanceService : ILeaveBalanceService
{
    private readonly ILeaveBalanceRepository _leaveBalanceRepository;
    private readonly ILeaveEmployeeAccessService _leaveEmployeeAccess;

    public LeaveBalanceService(
        ILeaveBalanceRepository leaveBalanceRepository,
        ILeaveEmployeeAccessService leaveEmployeeAccess)
    {
        _leaveBalanceRepository = leaveBalanceRepository;
        _leaveEmployeeAccess = leaveEmployeeAccess;
    }

    public async Task<IReadOnlyList<LeaveBalanceResponseModel>> GetByEmployeeAsync(
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        await _leaveEmployeeAccess.EnsureCanAccessEmployeeForLeaveAsync(employeeId, cancellationToken);

        var rows = await _leaveBalanceRepository.GetQueryable()
            .AsNoTracking()
            .Where(x => x.EmployeeId == employeeId)
            .OrderBy(x => x.LeaveTypeId)
            .ToListAsync(cancellationToken);

        return rows.Select(LeaveMapper.ToResponse).ToList();
    }

    public async Task<LeaveBalanceResponseModel> GetByEmployeeAndTypeAsync(
        int employeeId,
        int leaveTypeId,
        CancellationToken cancellationToken = default)
    {
        await _leaveEmployeeAccess.EnsureCanAccessEmployeeForLeaveAsync(employeeId, cancellationToken);

        var entity = await _leaveBalanceRepository.GetByEmployeeAndTypeAsync(
                employeeId,
                leaveTypeId,
                cancellationToken)
            ?? throw new BusinessRuleException("Leave balance was not found.");

        return LeaveMapper.ToResponse(entity);
    }
}
