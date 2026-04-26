using EMS.Application.DTOs.Attendance;

namespace EMS.Application.Services.Attendance;

public interface IAttendanceService
{
    Task<AttendanceRecordResponseModel> CheckInAsync(CheckInRequestModel request, CancellationToken cancellationToken = default);
    Task<AttendanceRecordResponseModel> CheckOutAsync(CheckOutRequestModel request, CancellationToken cancellationToken = default);
    Task<AttendanceBreakResponseModel> StartBreakAsync(StartBreakRequestModel request, CancellationToken cancellationToken = default);
    Task<AttendanceBreakResponseModel> EndBreakAsync(EndBreakRequestModel request, CancellationToken cancellationToken = default);
    Task<AttendanceRecordResponseModel> CreateManualEntryAsync(ManualAttendanceEntryRequestModel request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AttendanceRecordResponseModel>> GetEmployeeAttendanceAsync(
        int employeeId,
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken cancellationToken = default);
    Task<AttendanceSummaryResponseModel> GetSummaryAsync(
        int employeeId,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AttendanceDailySummaryResponseModel>> GetDailySummariesAsync(
        AttendanceReportFilterRequestModel request,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AttendancePeriodSummaryResponseModel>> GetPeriodSummariesAsync(
        AttendanceReportFilterRequestModel request,
        CancellationToken cancellationToken = default);
    Task<AttendancePunctualityResponseModel> GetPunctualityAnalyticsAsync(
        AttendanceReportFilterRequestModel request,
        CancellationToken cancellationToken = default);
    Task<AttendanceAbsenteeismResponseModel> GetAbsenteeismAnalyticsAsync(
        AttendanceReportFilterRequestModel request,
        CancellationToken cancellationToken = default);
    Task<AttendanceExportFileResponseModel> ExportReportAsync(
        AttendanceExportRequestModel request,
        CancellationToken cancellationToken = default);
}
