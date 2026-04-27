using EMS.Application.DTOs.Leave;
using EMS.Application.Services.Leave;
using Microsoft.AspNetCore.Mvc;

namespace EMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LeaveImportsController : ControllerBase
{
    private readonly ILeaveImportService _leaveImportService;

    public LeaveImportsController(ILeaveImportService leaveImportService)
    {
        _leaveImportService = leaveImportService;
    }

    [HttpPost("bulk")]
    public async Task<ActionResult<BulkLeaveImportResultResponseModel>> BulkImport(
        [FromBody] BulkLeaveImportRequestModel request,
        CancellationToken cancellationToken)
    {
        var result = await _leaveImportService.ImportAsync(request, cancellationToken);
        return Ok(result);
    }
}
