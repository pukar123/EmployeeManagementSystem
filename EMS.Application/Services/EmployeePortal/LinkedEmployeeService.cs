using System.Globalization;
using EMS.Application.Services.Authorization;
using EMS.Domain.DbModels;
using EMS.Domain.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using Pukar.Shared;

namespace EMS.Application.Services.EmployeePortal;

public sealed class LinkedEmployeeService : ILinkedEmployeeService
{
    private readonly IIdentityContext _identityContext;
    private readonly IBaseRepository<Employee> _employeeRepository;

    public LinkedEmployeeService(IIdentityContext identityContext, IBaseRepository<Employee> employeeRepository)
    {
        _identityContext = identityContext;
        _employeeRepository = employeeRepository;
    }

    public async Task<Employee?> TryGetLinkedEmployeeAsync(CancellationToken cancellationToken = default)
    {
        var identity = _identityContext.GetCurrent();
        if (identity.UserId is null)
            return null;

        var externalKey = identity.UserId.Value.ToString(CultureInfo.InvariantCulture);
        return await _employeeRepository.GetQueryable()
            .AsNoTracking()
            .Where(e => !e.IsArchived && e.ExternalIdentityKey == externalKey)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Employee> GetLinkedEmployeeOrThrowAsync(CancellationToken cancellationToken = default)
    {
        var identity = _identityContext.GetCurrent();
        if (identity.UserId is null)
            throw new BusinessRuleException("User is not authenticated.");

        var employee = await TryGetLinkedEmployeeAsync(cancellationToken);
        if (employee is null)
            throw new BusinessRuleException("No employee profile is linked to this user account.");

        return employee;
    }
}
