using EMS.Domain.Database;
using EMS.Domain.DbModels;
using EMS.Domain.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace EMS.Infrastructure.Repositories.Implementations;

public sealed class AttendanceBreakRepository : BaseRepository<AttendanceBreak>, IAttendanceBreakRepository
{
    public AttendanceBreakRepository(AppDbContext context) : base(context)
    {
    }

    public Task<AttendanceBreak?> GetOpenForAttendanceAsync(int attendanceRecordId, CancellationToken cancellationToken = default)
    {
        return Context.Set<AttendanceBreak>()
            .OrderByDescending(x => x.StartAtUtc)
            .FirstOrDefaultAsync(x => x.AttendanceRecordId == attendanceRecordId && x.EndAtUtc == null, cancellationToken);
    }
}
