using EMS.Domain.DbModels;

namespace EMS.Application.Services.EmployeePortal;

/// <summary>
/// Resolves the non-archived <see cref="Employee"/> linked to the current identity user via <c>ExternalIdentityKey</c>.
/// </summary>
public interface ILinkedEmployeeService
{
    Task<Employee?> TryGetLinkedEmployeeAsync(CancellationToken cancellationToken = default);

    Task<Employee> GetLinkedEmployeeOrThrowAsync(CancellationToken cancellationToken = default);
}
