using EMS.Domain.Database;
using EMS.Domain.DbModels;
using EMS.Domain.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace EMS.Infrastructure.Repositories.Implementations;

public sealed class LeaveTypeRepository : BaseRepository<LeaveType>, ILeaveTypeRepository
{
    public LeaveTypeRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<LeaveType>> GetActiveByOrganizationAsync(
        int organizationId,
        CancellationToken cancellationToken = default)
    {
        return await Context.Set<LeaveType>()
            .AsNoTracking()
            .Where(x => x.OrganizationId == organizationId && x.IsActive)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }
}
