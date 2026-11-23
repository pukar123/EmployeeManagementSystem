namespace EMS.Application.Services.Attendance;

/// <summary>
/// Who a check-in, check-out or break action is for, as decided by
/// <see cref="IAttendanceEmployeeAccessService.ResolveActionTargetAsync"/>.
/// </summary>
/// <param name="EmployeeId">The employee whose attendance the action changes.</param>
/// <param name="MayUseClientTime">
/// True only for attendance admins. Everyone else gets the server's current time, whatever time the request sends.
/// </param>
public sealed record AttendanceActionTarget(int EmployeeId, bool MayUseClientTime);
