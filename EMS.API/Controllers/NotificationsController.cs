using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pukar.Notifications.Application.DTOs;
using Pukar.Notifications.Application.Services;
using Pukar.Shared;

namespace EMS.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<NotificationResponseModel>>> GetAll(
        [FromQuery] NotificationQueryModel query,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _notificationService.GetForCurrentUserAsync(query, cancellationToken);
            return Ok(result);
        }
        catch (BusinessRuleException ex)
        {
            return HandleBusinessRule(ex);
        }
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult<NotificationUnreadCountResponseModel>> GetUnreadCount(
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _notificationService.GetUnreadCountAsync(cancellationToken);
            return Ok(result);
        }
        catch (BusinessRuleException ex)
        {
            return HandleBusinessRule(ex);
        }
    }

    [HttpPost("{id:int}/read")]
    public async Task<ActionResult<NotificationResponseModel>> MarkRead(
        int id,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _notificationService.MarkReadAsync(id, cancellationToken);
            return Ok(result);
        }
        catch (BusinessRuleException ex)
        {
            return HandleBusinessRule(ex);
        }
    }

    [HttpPost("read-all")]
    public async Task<ActionResult<object>> MarkAllRead(CancellationToken cancellationToken)
    {
        try
        {
            var count = await _notificationService.MarkAllReadAsync(cancellationToken);
            return Ok(new { count });
        }
        catch (BusinessRuleException ex)
        {
            return HandleBusinessRule(ex);
        }
    }

    private ActionResult HandleBusinessRule(BusinessRuleException ex)
    {
        if (ex.Message.Contains("not authenticated", StringComparison.OrdinalIgnoreCase))
            return Unauthorized(new { message = ex.Message });

        return EmployeeControllerHelpers.HandleBusinessRule(ex);
    }
}
