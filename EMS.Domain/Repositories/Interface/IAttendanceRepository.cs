using EMS.Domain.DbModels;

namespace EMS.Domain.Repositories.Interface;

public interface IAttendanceRepository : IBaseRepository<AttendanceRecord>
{
    Task<AttendanceRecord?> GetOpenForEmployeeAsync(int employeeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AttendanceRecord>> GetRecordsForReportAsync(
        int organizationId,
        DateTime fromDate,
        DateTime toDate,
        int? employeeId,
        int? departmentId,
        CancellationToken cancellationToken = default);
}
