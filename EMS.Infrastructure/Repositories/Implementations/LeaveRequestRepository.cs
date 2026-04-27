using EMS.Domain.Database;
using EMS.Domain.DbModels;
using EMS.Domain.Enums;
using EMS.Domain.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace EMS.Infrastructure.Repositories.Implementations;

public sealed class LeaveRequestRepository : BaseRepository<LeaveRequest>, ILeaveRequestRepository
{
    public LeaveRequestRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<LeaveRequest>> GetByEmployeeAsync(
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        return await Context.Set<LeaveRequest>()
            .AsNoTracking()
            .Where(x => x.EmployeeId == employeeId)
            .OrderByDescending(x => x.SubmittedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LeaveRequest>> GetByStatusAsync(
        int organizationId,
        LeaveRequestStatus status,
        CancellationToken cancellationToken = default)
    {
        return await Context.Set<LeaveRequest>()
            .AsNoTracking()
            .Where(x => x.OrganizationId == organizationId && x.Status == status)
            .OrderByDescending(x => x.SubmittedAtUtc)
            .ToListAsync(cancellationToken);
    }
}
