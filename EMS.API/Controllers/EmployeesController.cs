using EMS.Application.DTOs.Employee;
using EMS.Application.DTOs.Site;
using EMS.Application.Services.EmployeeSites;
using EMS.Application.Services.Employees;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pukar.Shared;

namespace EMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeService _employeeService;
    private readonly IEmployeeDirectoryService _employeeDirectoryService;
    private readonly IEmployeeLifecycleService _employeeLifecycleService;
    private readonly IEmployeeIdentityProvisioningService _employeeIdentityProvisioningService;
    private readonly IEmployeeSiteService _employeeSiteService;
    private readonly IEmployeeAccessService _employeeAccessService;
    private readonly IEmployeeTransferService _employeeTransferService;

    public EmployeesController(
        IEmployeeService employeeService,
        IEmployeeDirectoryService employeeDirectoryService,
        IEmployeeLifecycleService employeeLifecycleService,
        IEmployeeIdentityProvisioningService employeeIdentityProvisioningService,
        IEmployeeSiteService employeeSiteService,
        IEmployeeAccessService employeeAccessService,
        IEmployeeTransferService employeeTransferService)
    {
        _employeeService = employeeService;
        _employeeDirectoryService = employeeDirectoryService;
        _employeeLifecycleService = employeeLifecycleService;
        _employeeIdentityProvisioningService = employeeIdentityProvisioningService;
        _employeeSiteService = employeeSiteService;
        _employeeAccessService = employeeAccessService;
        _employeeTransferService = employeeTransferService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<EmployeeResponseModel>>> GetAll(
        CancellationToken cancellationToken,
        [FromQuery] bool includeArchived = false)
    {
        try
        {
            await _employeeAccessService.EnsureCanViewEmployeesAsync(cancellationToken);
            var items = await _employeeService.GetAllAsync(includeArchived, cancellationToken);
            return Ok(items);
        }
        catch (BusinessRuleException ex)
        {
            return EmployeeControllerHelpers.HandleBusinessRule(ex);
        }
    }

    [HttpGet("directory")]
    public async Task<ActionResult<PagedEmployeeDirectoryResponseModel>> GetDirectory(
        [FromQuery] EmployeeDirectoryQueryModel query,
        CancellationToken cancellationToken)
    {
        try
        {
            await _employeeAccessService.EnsureCanViewEmployeesAsync(cancellationToken);
            var result = await _employeeDirectoryService.QueryAsync(query, cancellationToken);
            return Ok(result);
        }
        catch (BusinessRuleException ex)
        {
            return EmployeeControllerHelpers.HandleBusinessRule(ex);
        }
    }

    [HttpGet("export")]
    public async Task<IActionResult> ExportDirectory(
        [FromQuery] EmployeeDirectoryQueryModel query,
        [FromQuery] string format = "csv",
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _employeeAccessService.EnsureCanViewEmployeesAsync(cancellationToken);
            if (!format.Trim().Equals("csv", StringComparison.OrdinalIgnoreCase))
                throw new BusinessRuleException("Only csv export is supported for the employee directory.");

            var file = await _employeeDirectoryService.ExportCsvAsync(query, cancellationToken);
            return File(file.Content, file.ContentType, file.FileName);
        }
        catch (BusinessRuleException ex)
        {
            return EmployeeControllerHelpers.HandleBusinessRule(ex);
        }
    }

    [HttpGet("possible-duplicates")]
    public async Task<ActionResult<IReadOnlyList<PossibleDuplicateEmployeeModel>>> GetPossibleDuplicates(
        [FromQuery] int organizationId,
        [FromQuery] string? email,
        [FromQuery] string? firstName,
        [FromQuery] string? lastName,
        [FromQuery] string? phoneNumber,
        [FromQuery] DateTime? dateOfBirth,
        CancellationToken cancellationToken)
    {
        try
        {
            await _employeeAccessService.EnsureCanManageEmployeesAsync(cancellationToken);
            var items = await _employeeService.FindPossibleDuplicatesAsync(
                organizationId,
                email,
                firstName,
                lastName,
                phoneNumber,
                dateOfBirth,
                cancellationToken);
            return Ok(items);
        }
        catch (BusinessRuleException ex)
        {
            return EmployeeControllerHelpers.HandleBusinessRule(ex);
        }
    }

    [HttpGet("{id:int}/profile")]
    public async Task<ActionResult<EmployeeProfileResponseModel>> GetProfile(
        int id,
        CancellationToken cancellationToken)
    {
        try
        {
            await _employeeAccessService.EnsureCanViewEmployeesAsync(cancellationToken);
            var profile = await _employeeService.GetProfileAsync(id, cancellationToken);
            return profile is null ? NotFound() : Ok(profile);
        }
        catch (BusinessRuleException ex)
        {
            return EmployeeControllerHelpers.HandleBusinessRule(ex);
        }
    }

    [HttpGet("{id:int}/sites")]
    public async Task<ActionResult<IReadOnlyList<SiteResponseModel>>> GetSitesForEmployee(
        int id,
        CancellationToken cancellationToken)
    {
        try
        {
            await _employeeAccessService.EnsureCanViewEmployeesAsync(cancellationToken);
            var items = await _employeeSiteService.GetSitesForEmployeeAsync(id, cancellationToken);
            return items is null ? NotFound() : Ok(items);
        }
        catch (BusinessRuleException ex)
        {
            return EmployeeControllerHelpers.HandleBusinessRule(ex);
        }
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<EmployeeResponseModel>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        try
        {
            await _employeeAccessService.EnsureCanViewEmployeesAsync(cancellationToken);
            var item = await _employeeService.GetByIdAsync(id, cancellationToken);
            return item is null ? NotFound() : Ok(item);
        }
        catch (BusinessRuleException ex)
        {
            return EmployeeControllerHelpers.HandleBusinessRule(ex);
        }
    }

    [HttpPost]
    public async Task<ActionResult<EmployeeResponseModel>> Create(
        [FromBody] CreateEmployeeRequestModel request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _employeeAccessService.EnsureCanManageEmployeesAsync(cancellationToken);
            var created = await _employeeService.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (BusinessRuleException ex)
        {
            return EmployeeControllerHelpers.HandleBusinessRule(ex);
        }
    }

    [HttpPost("{id:int}/provision-user")]
    public async Task<ActionResult<ProvisionEmployeeUserResponseModel>> ProvisionUser(
        int id,
        CancellationToken cancellationToken)
    {
        try
        {
            await _employeeAccessService.EnsureCanManageEmployeesAsync(cancellationToken);
            var response = await _employeeIdentityProvisioningService.ProvisionAsync(id, cancellationToken);
            return Ok(response);
        }
        catch (BusinessRuleException ex)
        {
            return EmployeeControllerHelpers.HandleBusinessRule(ex);
        }
    }

    [HttpPost("{id:int}/link-user")]
    public async Task<ActionResult<ProvisionEmployeeUserResponseModel>> LinkUser(
        int id,
        [FromBody] LinkEmployeeUserRequestModel request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _employeeAccessService.EnsureCanManageEmployeesAsync(cancellationToken);
            var response = await _employeeIdentityProvisioningService.LinkExistingUserAsync(id, request, cancellationToken);
            return Ok(response);
        }
        catch (BusinessRuleException ex)
        {
            return EmployeeControllerHelpers.HandleBusinessRule(ex);
        }
    }

    [HttpPut("{id:int}/linked-user/roles")]
    public async Task<IActionResult> AssignLinkedUserRoles(
        int id,
        [FromBody] AssignEmployeeUserRolesRequestModel request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _employeeAccessService.EnsureCanManageEmployeesAsync(cancellationToken);
            await _employeeIdentityProvisioningService.AssignRolesAsync(id, request, cancellationToken);
            return NoContent();
        }
        catch (BusinessRuleException ex)
        {
            return EmployeeControllerHelpers.HandleBusinessRule(ex);
        }
    }

    [HttpPost("{id:int}/transfers/department")]
    public async Task<ActionResult<EmployeeResponseModel>> TransferDepartment(
        int id,
        [FromBody] TransferEmployeeDepartmentRequestModel request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _employeeAccessService.EnsureCanManageEmployeesAsync(cancellationToken);
            var updated = await _employeeTransferService.TransferDepartmentAsync(id, request, cancellationToken);
            return updated is null ? NotFound() : Ok(updated);
        }
        catch (BusinessRuleException ex)
        {
            return EmployeeControllerHelpers.HandleBusinessRule(ex);
        }
    }

    [HttpPost("{id:int}/transfers/position")]
    public async Task<ActionResult<EmployeeResponseModel>> TransferPosition(
        int id,
        [FromBody] TransferEmployeePositionRequestModel request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _employeeAccessService.EnsureCanManageEmployeesAsync(cancellationToken);
            var updated = await _employeeTransferService.TransferPositionAsync(id, request, cancellationToken);
            return updated is null ? NotFound() : Ok(updated);
        }
        catch (BusinessRuleException ex)
        {
            return EmployeeControllerHelpers.HandleBusinessRule(ex);
        }
    }

    [HttpPost("{id:int}/transfers/manager")]
    public async Task<ActionResult<EmployeeResponseModel>> TransferManager(
        int id,
        [FromBody] TransferEmployeeManagerRequestModel request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _employeeAccessService.EnsureCanManageEmployeesAsync(cancellationToken);
            var updated = await _employeeTransferService.TransferManagerAsync(id, request, cancellationToken);
            return updated is null ? NotFound() : Ok(updated);
        }
        catch (BusinessRuleException ex)
        {
            return EmployeeControllerHelpers.HandleBusinessRule(ex);
        }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<EmployeeResponseModel>> Update(
        int id,
        [FromBody] UpdateEmployeeRequestModel request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _employeeAccessService.EnsureCanManageEmployeesAsync(cancellationToken);
            var updated = await _employeeService.UpdateAsync(id, request, cancellationToken);
            return updated is null ? NotFound() : Ok(updated);
        }
        catch (BusinessRuleException ex)
        {
            return EmployeeControllerHelpers.HandleBusinessRule(ex);
        }
    }

    [HttpPost("{id:int}/terminate")]
    public async Task<ActionResult<EmployeeResponseModel>> Terminate(
        int id,
        [FromBody] TerminateEmployeeRequestModel request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _employeeAccessService.EnsureCanManageEmployeesAsync(cancellationToken);
            var updated = await _employeeLifecycleService.TerminateAsync(id, request, cancellationToken);
            return updated is null ? NotFound() : Ok(updated);
        }
        catch (BusinessRuleException ex)
        {
            return EmployeeControllerHelpers.HandleBusinessRule(ex);
        }
    }

    [HttpPost("{id:int}/archive")]
    public async Task<ActionResult<EmployeeResponseModel>> Archive(
        int id,
        [FromBody] ArchiveEmployeeRequestModel request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _employeeAccessService.EnsureCanManageEmployeesAsync(cancellationToken);
            var updated = await _employeeLifecycleService.ArchiveAsync(id, request, cancellationToken);
            return updated is null ? NotFound() : Ok(updated);
        }
        catch (BusinessRuleException ex)
        {
            return EmployeeControllerHelpers.HandleBusinessRule(ex);
        }
    }

    [HttpPost("{id:int}/restore")]
    public async Task<ActionResult<EmployeeResponseModel>> Restore(int id, CancellationToken cancellationToken)
    {
        try
        {
            await _employeeAccessService.EnsureCanManageEmployeesAsync(cancellationToken);
            var updated = await _employeeLifecycleService.RestoreAsync(id, cancellationToken);
            return updated is null ? NotFound() : Ok(updated);
        }
        catch (BusinessRuleException ex)
        {
            return EmployeeControllerHelpers.HandleBusinessRule(ex);
        }
    }

    [HttpPost("{id:int}/employment-status")]
    public async Task<ActionResult<EmployeeResponseModel>> ChangeEmploymentStatus(
        int id,
        [FromBody] ChangeEmploymentStatusRequestModel request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _employeeAccessService.EnsureCanManageEmployeesAsync(cancellationToken);
            var updated = await _employeeLifecycleService.ChangeEmploymentStatusAsync(id, request, cancellationToken);
            return updated is null ? NotFound() : Ok(updated);
        }
        catch (BusinessRuleException ex)
        {
            return EmployeeControllerHelpers.HandleBusinessRule(ex);
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(
        int id,
        [FromBody] ArchiveEmployeeRequestModel? request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _employeeAccessService.EnsureCanManageEmployeesAsync(cancellationToken);
            var archived = await _employeeService.DeleteAsync(id, request, cancellationToken);
            return archived ? NoContent() : NotFound();
        }
        catch (BusinessRuleException ex)
        {
            return EmployeeControllerHelpers.HandleBusinessRule(ex);
        }
    }
}
