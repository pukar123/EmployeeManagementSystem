using Microsoft.EntityFrameworkCore;
using Pukar.Shared;

namespace EMS.Application.Services.Employees;

public static class EmployeePersistenceExceptionMapper
{
    public static BusinessRuleException MapDbUpdateException(DbUpdateException exception)
    {
        var message = exception.InnerException?.Message ?? exception.Message;

        if (message.Contains("EmployeeNumber", StringComparison.OrdinalIgnoreCase)
            || message.Contains("IX_Employees", StringComparison.OrdinalIgnoreCase)
            && message.Contains("EmployeeNumber", StringComparison.OrdinalIgnoreCase))
        {
            return new BusinessRuleException("Employee number allocation conflict; please retry.");
        }

        if (message.Contains("Email", StringComparison.OrdinalIgnoreCase)
            || message.Contains("duplicate", StringComparison.OrdinalIgnoreCase))
        {
            return new BusinessRuleException("An employee with this email already exists.");
        }

        if (message.Contains("FOREIGN KEY", StringComparison.OrdinalIgnoreCase)
            || message.Contains("REFERENCE constraint", StringComparison.OrdinalIgnoreCase))
        {
            return new BusinessRuleException("One or more related records are invalid.");
        }

        return new BusinessRuleException("Could not save employee record due to a database constraint.");
    }
}
