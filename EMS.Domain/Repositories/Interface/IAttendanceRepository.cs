using EMS.Domain.DbModels;

namespace EMS.Domain.Repositories.Interface;

public interface IAttendanceRepository : IBaseRepository<AttendanceRecord>
{
    Task<AttendanceRecord?> GetOpenForEmployeeAsync(int employeeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records for a report in one organisation and date range. <paramref name="allowedEmployeeIds"/>, when given,
    /// limits the rows to those employees in the query itself; <paramref name="employeeId"/> and
    /// <paramref name="departmentId"/> narrow further.
    /// </summary>
    Task<IReadOnlyList<AttendanceRecord>> GetRecordsForReportAsync(
        int organizationId,
        DateTime fromDate,
        DateTime toDate,
        int? employeeId,
        int? departmentId,
        IReadOnlyCollection<int>? allowedEmployeeIds,
        CancellationToken cancellationToken = default);
}
