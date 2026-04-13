using EMS.Application.DTOs.Attendance;
using EMS.Application.Services.Attendance;
using Microsoft.AspNetCore.Mvc;
using Pukar.Shared;

namespace EMS.API.Controllers;

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
            return Conflict(new { message = ex.Message });
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
            return Conflict(new { message = ex.Message });
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
            return Conflict(new { message = ex.Message });
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
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPost("manual-entry")]
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
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpGet("employee/{employeeId:int}")]
    public async Task<ActionResult<IReadOnlyList<AttendanceRecordResponseModel>>> GetEmployeeAttendance(
        int employeeId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken cancellationToken)
    {
        var result = await _attendanceService.GetEmployeeAttendanceAsync(employeeId, fromDate, toDate, cancellationToken);
        return Ok(result);
    }

    [HttpGet("summary")]
    public async Task<ActionResult<AttendanceSummaryResponseModel>> GetSummary(
        [FromQuery] int employeeId,
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate,
        CancellationToken cancellationToken)
    {
        var result = await _attendanceService.GetSummaryAsync(employeeId, fromDate, toDate, cancellationToken);
        return Ok(result);
    }
}
