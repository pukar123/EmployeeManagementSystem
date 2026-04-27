using EMS.Domain.Database;
using EMS.Domain.DbModels;
using EMS.Domain.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace EMS.Infrastructure.Repositories.Implementations;

public sealed class LeaveBalanceRepository : BaseRepository<LeaveBalance>, ILeaveBalanceRepository
{
    public LeaveBalanceRepository(AppDbContext context) : base(context)
    {
    }

    public Task<LeaveBalance?> GetByEmployeeAndTypeAsync(
        int employeeId,
        int leaveTypeId,
        CancellationToken cancellationToken = default)
    {
        return Context.Set<LeaveBalance>()
            .FirstOrDefaultAsync(
                x => x.EmployeeId == employeeId && x.LeaveTypeId == leaveTypeId,
                cancellationToken);
    }
}
