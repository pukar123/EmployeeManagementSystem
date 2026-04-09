using EMS.Application.DTOs.Department;
using EMS.Application.Mapping;
using Pukar.Shared;
using EMS.Application.Services.Departments;
using Microsoft.AspNetCore.Mvc;

namespace EMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DepartmentsController : ControllerBase
{
    private readonly IDepartmentService _departmentService;

    public DepartmentsController(IDepartmentService departmentService)
    {
        _departmentService = departmentService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DepartmentResponseModel>>> GetAll(CancellationToken cancellationToken)
    {
        var items = await _departmentService.GetAllAsync(cancellationToken);
        return Ok(items);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<DepartmentResponseModel>> GetById(int id, CancellationToken cancellationToken)
    {
        var item = await _departmentService.GetByIdAsync(id, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<DepartmentResponseModel>> Create(
        [FromBody] DepartmentDTO dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var created = await _departmentService.CreateAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created.ToResponseModel());
        }
        catch (BusinessRuleException ex)
        {
            return Conflict(ex.Message);
        }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<DepartmentResponseModel>> Update(
        int id,
        [FromBody] UpdateDepartmentRequestModel request,
        CancellationToken cancellationToken)
    {
        var updated = await _departmentService.UpdateAsync(id, request, cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var deleted = await _departmentService.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
