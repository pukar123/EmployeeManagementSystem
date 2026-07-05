using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using EMS.Application.DTOs.Task;
using EMS.Application.Services.EmployeePortal;
using EMS.Application.Services.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pukar.Shared;
using Pukar.Usermanagement.Contracts.Roles;

namespace EMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;
    private readonly ILinkedEmployeeService _linkedEmployeeService;

    public TasksController(ITaskService taskService, ILinkedEmployeeService linkedEmployeeService)
    {
        _taskService = taskService;
        _linkedEmployeeService = linkedEmployeeService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TaskResponseModel>>> GetAll(
        [FromQuery] int? employeeId,
        [FromQuery] int? assignedByUserId,
        [FromQuery] DateTime? rangeStartUtc,
        [FromQuery] DateTime? rangeEndUtc,
        CancellationToken cancellationToken)
    {
        int? effectiveEmployeeId;
        if (IsAdmin())
        {
            effectiveEmployeeId = employeeId;
        }
        else
        {
            // Tasks are keyed by EMS Employee.Id, not the identity user id; resolve the linked employee.
            var linkedEmployee = await _linkedEmployeeService.TryGetLinkedEmployeeAsync(cancellationToken);
            if (linkedEmployee is null)
                return Ok(Array.Empty<TaskResponseModel>());

            if (employeeId.HasValue && employeeId.Value != linkedEmployee.Id)
                return Forbid();

            effectiveEmployeeId = linkedEmployee.Id;
        }

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

        if (!await CanAccessTaskAsync(task, cancellationToken))
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

            if (!await CanAccessTaskAsync(existing, cancellationToken))
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

    private async Task<bool> CanAccessTaskAsync(TaskResponseModel task, CancellationToken cancellationToken)
    {
        if (IsAdmin())
            return true;

        var currentUserId = GetCurrentUserId();
        if (currentUserId.HasValue && task.AssignedByUserId == currentUserId.Value)
            return true;

        // task.EmployeeId is an EMS Employee.Id; compare against the employee linked to the current user.
        var linkedEmployee = await _linkedEmployeeService.TryGetLinkedEmployeeAsync(cancellationToken);
        return linkedEmployee is not null && task.EmployeeId == linkedEmployee.Id;
    }
}
