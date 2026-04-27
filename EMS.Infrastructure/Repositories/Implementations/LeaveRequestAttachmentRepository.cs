using EMS.Domain.Database;
using EMS.Domain.DbModels;
using EMS.Domain.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace EMS.Infrastructure.Repositories.Implementations;

public sealed class LeaveRequestAttachmentRepository : BaseRepository<LeaveRequestAttachment>, ILeaveRequestAttachmentRepository
{
    public LeaveRequestAttachmentRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<LeaveRequestAttachment>> GetByRequestIdAsync(
        int leaveRequestId,
        CancellationToken cancellationToken = default)
    {
        return await Context.Set<LeaveRequestAttachment>()
            .AsNoTracking()
            .Where(x => x.LeaveRequestId == leaveRequestId)
            .OrderBy(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }
}
