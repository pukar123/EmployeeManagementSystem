using EMS.Application.DTOs.Attendance;
using EMS.Domain.DbModels;

namespace EMS.Application.Mapping;

internal static class AttendanceMapper
{
    public static AttendanceBreakResponseModel ToResponse(AttendanceBreak entity)
    {
        return new AttendanceBreakResponseModel
        {
            Id = entity.Id,
            AttendanceRecordId = entity.AttendanceRecordId,
            StartAtUtc = entity.StartAtUtc,
            EndAtUtc = entity.EndAtUtc,
            DurationMinutes = entity.DurationMinutes,
        };
    }

    public static AttendanceRecordResponseModel ToResponse(AttendanceRecord entity)
    {
        var breaks = entity.Breaks
            .OrderBy(x => x.StartAtUtc)
            .Select(ToResponse)
            .ToList();
        var breakMinutes = breaks.Sum(x => x.DurationMinutes ?? 0);

        var worked = entity.CheckOutAtUtc.HasValue
            ? Math.Max(0, (int)Math.Round((entity.CheckOutAtUtc.Value - entity.CheckInAtUtc).TotalMinutes) - breakMinutes)
            : 0;

        return new AttendanceRecordResponseModel
        {
            Id = entity.Id,
            OrganizationId = entity.OrganizationId,
            EmployeeId = entity.EmployeeId,
            WorkDate = entity.WorkDate,
            CheckInAtUtc = entity.CheckInAtUtc,
            CheckOutAtUtc = entity.CheckOutAtUtc,
            CheckInLatitude = entity.CheckInLatitude,
            CheckInLongitude = entity.CheckInLongitude,
            CheckOutLatitude = entity.CheckOutLatitude,
            CheckOutLongitude = entity.CheckOutLongitude,
            Source = entity.Source,
            Status = entity.Status,
            ManualReason = entity.ManualReason,
            BreakMinutes = breakMinutes,
            WorkedMinutes = worked,
            Breaks = breaks,
            CreatedAtUtc = entity.CreatedAtUtc,
            UpdatedAtUtc = entity.UpdatedAtUtc,
        };
    }
}
