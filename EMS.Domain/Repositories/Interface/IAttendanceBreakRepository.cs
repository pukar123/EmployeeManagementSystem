using EMS.Domain.DbModels;

namespace EMS.Domain.Repositories.Interface;

public interface IAttendanceBreakRepository : IBaseRepository<AttendanceBreak>
{
    Task<AttendanceBreak?> GetOpenForAttendanceAsync(int attendanceRecordId, CancellationToken cancellationToken = default);
}
