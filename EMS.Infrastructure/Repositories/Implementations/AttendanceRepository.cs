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

    public async Task<IReadOnlyList<AttendanceRecord>> GetRecordsForReportAsync(
        int organizationId,
        DateTime fromDate,
        DateTime toDate,
        int? employeeId,
        int? departmentId,
        CancellationToken cancellationToken = default)
    {
        var query = Context.Set<AttendanceRecord>()
            .AsNoTracking()
            .Include(x => x.Breaks)
            .Include(x => x.Employee)
            .Where(x =>
                x.OrganizationId == organizationId &&
                x.WorkDate >= fromDate.Date &&
                x.WorkDate <= toDate.Date);

        if (employeeId.HasValue)
            query = query.Where(x => x.EmployeeId == employeeId.Value);

        if (departmentId.HasValue)
            query = query.Where(x => x.Employee.DepartmentId == departmentId.Value);

        return await query
            .OrderBy(x => x.WorkDate)
            .ThenBy(x => x.EmployeeId)
            .ThenBy(x => x.CheckInAtUtc)
            .ToListAsync(cancellationToken);
    }
}
