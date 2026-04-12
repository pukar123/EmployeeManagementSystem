using EMS.Application.DTOs.Navigation;
using EMS.Application.Services.Menus;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Pukar.Shared;

namespace EMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class MenusController : ControllerBase
{
    private readonly IMenuService _menus;

    public MenusController(IMenuService menus)
    {
        _menus = menus;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<MenuResponseModel>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MenuResponseModel>>> GetAll(CancellationToken cancellationToken)
    {
        var items = await _menus.GetAllFlatAsync(cancellationToken);
        return Ok(items);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(MenuResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MenuResponseModel>> GetById(int id, CancellationToken cancellationToken)
    {
        var item = await _menus.GetByIdAsync(id, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    [ProducesResponseType(typeof(MenuResponseModel), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MenuResponseModel>> Create(
        [FromBody] CreateMenuRequestModel request,
        CancellationToken cancellationToken)
    {
        try
        {
            var created = await _menus.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (BusinessRuleException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(MenuResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MenuResponseModel>> Update(
        int id,
        [FromBody] UpdateMenuRequestModel request,
        CancellationToken cancellationToken)
    {
        try
        {
            var updated = await _menus.UpdateAsync(id, request, cancellationToken);
            return updated is null ? NotFound() : Ok(updated);
        }
        catch (BusinessRuleException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await _menus.DeleteAsync(id, cancellationToken);
            return deleted ? NoContent() : NotFound();
        }
        catch (BusinessRuleException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}
