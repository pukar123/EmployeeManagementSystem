using System.Globalization;
using EMS.Domain.DbModels;
using EMS.Domain.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace EMS.Application.Services.Notifications;

public sealed class EmployeeNotificationRecipientResolver
{
    private readonly IBaseRepository<Employee> _employees;

    public EmployeeNotificationRecipientResolver(IBaseRepository<Employee> employees)
    {
        _employees = employees;
    }

    public static int? TryResolveUserId(Employee employee)
    {
        if (string.IsNullOrWhiteSpace(employee.ExternalIdentityKey))
            return null;

        return int.TryParse(employee.ExternalIdentityKey, NumberStyles.Integer, CultureInfo.InvariantCulture, out var userId)
            ? userId
            : null;
    }

    public async Task<int?> TryResolveUserIdByEmployeeIdAsync(
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        var employee = await _employees.GetQueryable()
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == employeeId && !e.IsArchived, cancellationToken);

        return employee is null ? null : TryResolveUserId(employee);
    }
}
