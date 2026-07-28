namespace EMS.Application.Services.Employees;

public interface IEmployeeNumberAllocator
{
    Task<string> AllocateAsync(int organizationId, CancellationToken cancellationToken = default);
}
