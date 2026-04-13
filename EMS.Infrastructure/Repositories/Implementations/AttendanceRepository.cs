using EMS.Domain.Database;
using EMS.Domain.DbModels;
using EMS.Domain.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace EMS.Infrastructure.Repositories.Implementations;

public sealed class AttendanceRepository : BaseRepository<AttendanceRecord>, IAttendanceRepository
{
    public AttendanceRepository(AppDbContext context) : base(context)
    {
    }

    public Task<AttendanceRecord?> GetOpenForEmployeeAsync(int employeeId, CancellationToken cancellationToken = default)
    {
        return Context.Set<AttendanceRecord>()
            .OrderByDescending(x => x.CheckInAtUtc)
            .FirstOrDefaultAsync(x => x.EmployeeId == employeeId && x.CheckOutAtUtc == null, cancellationToken);
    }
}
