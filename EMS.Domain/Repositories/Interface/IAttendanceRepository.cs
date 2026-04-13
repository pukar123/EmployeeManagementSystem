using EMS.Domain.DbModels;

namespace EMS.Domain.Repositories.Interface;

public interface IAttendanceRepository : IBaseRepository<AttendanceRecord>
{
    Task<AttendanceRecord?> GetOpenForEmployeeAsync(int employeeId, CancellationToken cancellationToken = default);
}
