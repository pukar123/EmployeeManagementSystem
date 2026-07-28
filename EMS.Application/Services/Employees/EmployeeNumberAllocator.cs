using System.Globalization;
using EMS.Domain.DbModels;
using EMS.Domain.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using Pukar.Shared;

namespace EMS.Application.Services.Employees;

public sealed class EmployeeNumberAllocator : IEmployeeNumberAllocator
{
    private readonly IBaseRepository<OrganizationEmployeeNumberSequence> _sequences;

    public EmployeeNumberAllocator(IBaseRepository<OrganizationEmployeeNumberSequence> sequences)
    {
        _sequences = sequences;
    }

    public async Task<string> AllocateAsync(int organizationId, CancellationToken cancellationToken = default)
    {
        var sequence = await _sequences.GetQueryable()
            .FirstOrDefaultAsync(s => s.OrganizationId == organizationId, cancellationToken);

        if (sequence is null)
        {
            sequence = new OrganizationEmployeeNumberSequence
            {
                OrganizationId = organizationId,
                LastAllocatedNumber = 0,
            };
            await _sequences.AddAsync(sequence, cancellationToken);
        }

        sequence.LastAllocatedNumber++;
        _sequences.Update(sequence);
        await _sequences.SaveChangesAsync(cancellationToken);

        return FormatEmployeeNumber(sequence.LastAllocatedNumber);
    }

    public static string FormatEmployeeNumber(int number)
        => $"EMP{number.ToString("D3", CultureInfo.InvariantCulture)}";
}
