using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using EMS.Application.DTOs.Task;
using EMS.Application.Services.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pukar.Shared;
using Pukar.Usermanagement.Application;

namespace EMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;

    public TasksController(ITaskService taskService)
    {
        _taskService = taskService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TaskResponseModel>>> GetAll(
        [FromQuery] int? employeeId,
        [FromQuery] int? assignedByUserId,
        [FromQuery] DateTime? rangeStartUtc,
        [FromQuery] DateTime? rangeEndUtc,
        CancellationToken cancellationToken)
    {
        if (!IsAdmin() && employeeId.HasValue && employeeId.Value != GetCurrentUserId())
            return Forbid();

        var effectiveEmployeeId = IsAdmin() ? employeeId : GetCurrentUserId();
        try
        {
            var items = await _taskService.GetAllAsync(effectiveEmployeeId, assignedByUserId, rangeStartUtc, rangeEndUtc, cancellationToken);
            return Ok(items);
        }
        catch (BusinessRuleException ex)
        {
            return Conflict(ex.Message);
        }
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TaskResponseModel>> GetById(int id, CancellationToken cancellationToken)
    {
        var task = await _taskService.GetByIdAsync(id, cancellationToken);
        if (task is null)
            return NotFound();

        if (!CanAccessTask(task))
            return Forbid();

        return Ok(task);
    }

    [HttpPost]
    [Authorize(Roles = WellKnownRoles.Admin)]
    public async Task<ActionResult<TaskResponseModel>> Create(
        [FromBody] CreateTaskRequestModel request,
        CancellationToken cancellationToken)
    {
        try
        {
            var created = await _taskService.CreateAsync(request, GetCurrentUserId(), cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (BusinessRuleException ex)
        {
            return Conflict(ex.Message);
        }
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = WellKnownRoles.Admin)]
    public async Task<ActionResult<TaskResponseModel>> Update(
        int id,
        [FromBody] UpdateTaskRequestModel request,
        CancellationToken cancellationToken)
    {
        try
        {
            var updated = await _taskService.UpdateAsync(id, request, cancellationToken);
            return updated is null ? NotFound() : Ok(updated);
        }
        catch (BusinessRuleException ex)
        {
            return Conflict(ex.Message);
        }
    }

    [HttpPatch("{id:int}/status")]
    public async Task<ActionResult<TaskResponseModel>> UpdateStatus(
        int id,
        [FromBody] UpdateTaskStatusRequestModel request,
        CancellationToken cancellationToken)
    {
        try
        {
            var existing = await _taskService.GetByIdAsync(id, cancellationToken);
            if (existing is null)
                return NotFound();

            if (!CanAccessTask(existing))
                return Forbid();

            var updated = await _taskService.UpdateStatusAsync(id, request, cancellationToken);
            return updated is null ? NotFound() : Ok(updated);
        }
        catch (BusinessRuleException ex)
        {
            return Conflict(ex.Message);
        }
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = WellKnownRoles.Admin)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var deleted = await _taskService.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    private int? GetCurrentUserId()
    {
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                  ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(sub, out var id) ? id : null;
    }

    private bool IsAdmin() => User.IsInRole(WellKnownRoles.Admin);

    private bool CanAccessTask(TaskResponseModel task)
    {
        if (IsAdmin())
            return true;

        var currentUserId = GetCurrentUserId();
        return currentUserId.HasValue
            && (task.AssignedByUserId == currentUserId.Value || task.EmployeeId == currentUserId.Value);
    }
}
