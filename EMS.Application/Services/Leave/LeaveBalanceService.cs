using EMS.Application.DTOs.Leave;
using EMS.Application.Mapping;
using EMS.Domain.DbModels;
using EMS.Domain.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using Pukar.Shared;

namespace EMS.Application.Services.Leave;

public sealed class LeaveBalanceService : ILeaveBalanceService
{
    private readonly IBaseRepository<LeaveBalance> _leaveBalanceRepository;

    public LeaveBalanceService(IBaseRepository<LeaveBalance> leaveBalanceRepository)
    {
        _leaveBalanceRepository = leaveBalanceRepository;
    }

    public async Task<IReadOnlyList<LeaveBalanceResponseModel>> GetByEmployeeAsync(
        int employeeId,
        CancellationToken cancellationToken = default)
    {
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
        var entity = await _leaveBalanceRepository.GetQueryable()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.EmployeeId == employeeId && x.LeaveTypeId == leaveTypeId,
                cancellationToken)
            ?? throw new BusinessRuleException("Leave balance was not found.");

        return LeaveMapper.ToResponse(entity);
    }
}
