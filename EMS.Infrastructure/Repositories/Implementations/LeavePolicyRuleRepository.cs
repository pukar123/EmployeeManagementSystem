using EMS.Domain.Database;
using EMS.Domain.DbModels;
using EMS.Domain.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace EMS.Infrastructure.Repositories.Implementations;

public sealed class LeavePolicyRuleRepository : BaseRepository<LeavePolicyRule>, ILeavePolicyRuleRepository
{
    public LeavePolicyRuleRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<LeavePolicyRule>> GetByLeaveTypeAsync(
        int leaveTypeId,
        CancellationToken cancellationToken = default)
    {
        return await Context.Set<LeavePolicyRule>()
            .AsNoTracking()
            .Where(x => x.LeaveTypeId == leaveTypeId)
            .OrderByDescending(x => x.EffectiveFromDateUtc)
            .ToListAsync(cancellationToken);
    }
}
