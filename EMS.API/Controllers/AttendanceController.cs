using EMS.API.Auth;
using EMS.Application.DTOs.Attendance;
using EMS.Application.Services.Attendance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pukar.Shared;

namespace EMS.API.Controllers;

/// <summary>
/// Attendance actions, reads and reports. Who may do what is decided in the Application layer (B07); this controller
/// only maps access denials to 403 before its usual status codes.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AttendanceController : ControllerBase
{
    private readonly IAttendanceService _attendanceService;

    public AttendanceController(IAttendanceService attendanceService)
    {
        _attendanceService = attendanceService;
    }

    [HttpPost("check-in")]
    public async Task<ActionResult<AttendanceRecordResponseModel>> CheckIn(
        [FromBody] CheckInRequestModel request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _attendanceService.CheckInAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (BusinessRuleException ex)
        {
            return HandleActionError(ex);
        }
    }

    [HttpPost("check-out")]
    public async Task<ActionResult<AttendanceRecordResponseModel>> CheckOut(
        [FromBody] CheckOutRequestModel request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _attendanceService.CheckOutAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (BusinessRuleException ex)
        {
            return HandleActionError(ex);
        }
    }

    [HttpPost("break/start")]
    public async Task<ActionResult<AttendanceBreakResponseModel>> StartBreak(
        [FromBody] StartBreakRequestModel request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _attendanceService.StartBreakAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (BusinessRuleException ex)
        {
            return HandleActionError(ex);
        }
    }

    [HttpPost("break/end")]
    public async Task<ActionResult<AttendanceBreakResponseModel>> EndBreak(
        [FromBody] EndBreakRequestModel request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _attendanceService.EndBreakAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (BusinessRuleException ex)
        {
            return HandleActionError(ex);
        }
    }

    [HttpPost("manual-entry")]
    [Authorize(Policy = AuthorizationPolicyNames.AttendanceAdmin)]
    public async Task<ActionResult<AttendanceRecordResponseModel>> ManualEntry(
        [FromBody] ManualAttendanceEntryRequestModel request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _attendanceService.CreateManualEntryAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (BusinessRuleException ex)
        {
            return HandleActionError(ex);
        }
    }

    [HttpGet("employee/{employeeId:int}")]
    public async Task<ActionResult<IReadOnlyList<AttendanceRecordResponseModel>>> GetEmployeeAttendance(
        int employeeId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _attendanceService.GetEmployeeAttendanceAsync(employeeId, fromDate, toDate, cancellationToken);
            return Ok(result);
        }
        catch (BusinessRuleException ex) when (IsAccessDenied(ex))
        {
            return Forbid();
        }
    }

    [HttpGet("summary")]
    public async Task<ActionResult<AttendanceSummaryResponseModel>> GetSummary(
        [FromQuery] int employeeId,
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _attendanceService.GetSummaryAsync(employeeId, fromDate, toDate, cancellationToken);
            return Ok(result);
        }
        catch (BusinessRuleException ex) when (IsAccessDenied(ex))
        {
            return Forbid();
        }
    }

    [HttpGet("reports/daily")]
    public async Task<ActionResult<IReadOnlyList<AttendanceDailySummaryResponseModel>>> GetDailySummaries(
        [FromQuery] AttendanceReportFilterRequestModel request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _attendanceService.GetDailySummariesAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (BusinessRuleException ex)
        {
            return HandleReportError(ex);
        }
    }

    [HttpGet("reports/period")]
    public async Task<ActionResult<IReadOnlyList<AttendancePeriodSummaryResponseModel>>> GetPeriodSummaries(
        [FromQuery] AttendanceReportFilterRequestModel request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _attendanceService.GetPeriodSummariesAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (BusinessRuleException ex)
        {
            return HandleReportError(ex);
        }
    }

    [HttpGet("analytics/punctuality")]
    public async Task<ActionResult<AttendancePunctualityResponseModel>> GetPunctualityAnalytics(
        [FromQuery] AttendanceReportFilterRequestModel request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _attendanceService.GetPunctualityAnalyticsAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (BusinessRuleException ex)
        {
            return HandleReportError(ex);
        }
    }

    [HttpGet("analytics/absenteeism")]
    public async Task<ActionResult<AttendanceAbsenteeismResponseModel>> GetAbsenteeismAnalytics(
        [FromQuery] AttendanceReportFilterRequestModel request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _attendanceService.GetAbsenteeismAnalyticsAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (BusinessRuleException ex)
        {
            return HandleReportError(ex);
        }
    }

    [HttpGet("reports/export")]
    public async Task<IActionResult> ExportReport(
        [FromQuery] AttendanceExportRequestModel request,
        CancellationToken cancellationToken)
    {
        try
        {
            var fileData = await _attendanceService.ExportReportAsync(request, cancellationToken);
            return File(fileData.FileBytes, fileData.ContentType, fileData.FileName);
        }
        catch (BusinessRuleException ex)
        {
            return HandleReportError(ex);
        }
    }

    /// <summary>Self actions and manual entries: 403 for an access denial, 400 for "choose an employee", otherwise 409.</summary>
    private ActionResult HandleActionError(BusinessRuleException ex)
    {
        if (IsAccessDenied(ex))
            return Forbid();

        if (string.Equals(ex.Message, AttendanceAccessMessages.ChooseEmployee, StringComparison.Ordinal))
            return BadRequest(new { message = ex.Message });

        return Conflict(new { message = ex.Message });
    }

    /// <summary>Reports, analytics and exports: 403 for an access denial, otherwise 400.</summary>
    private ActionResult HandleReportError(BusinessRuleException ex)
    {
        if (IsAccessDenied(ex))
            return Forbid();

        return BadRequest(new { message = ex.Message });
    }

    private static bool IsAccessDenied(BusinessRuleException ex)
        => string.Equals(ex.Message, AttendanceAccessMessages.Denied, StringComparison.Ordinal);
}
