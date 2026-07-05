using EMS.Application.Services.Employees;
using EMS.Application.Services.Integrations;
using Microsoft.AspNetCore.Mvc;
using Pukar.Shared;

namespace EMS.API.Controllers;

internal static class EmployeeControllerHelpers
{
    public static ActionResult HandleBusinessRule(BusinessRuleException ex)
    {
        if (string.Equals(ex.Message, EmployeeAccessMessages.Denied, StringComparison.Ordinal))
            return new ForbidResult();

        if (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
            return new NotFoundObjectResult(new { message = ex.Message });

        if (ex.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase)
            || ex.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase)
            || ex.Message.Contains("allocation conflict", StringComparison.OrdinalIgnoreCase))
        {
            return new ConflictObjectResult(new { message = ex.Message });
        }

        return new BadRequestObjectResult(new { message = ex.Message });
    }

    public static ActionResult HandleDependencyUnavailable(UserManagementDependencyUnavailableException ex)
        => new ObjectResult(new
        {
            message = ex.Message,
            code = "user_management_unavailable",
        })
        {
            StatusCode = StatusCodes.Status503ServiceUnavailable,
        };
}
