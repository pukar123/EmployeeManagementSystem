using System.Globalization;
using EMS.Domain.Database;
using EMS.Application.Services.Employees;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Pukar.Shared;

namespace EMS.Infrastructure.Repositories.Implementations;

public sealed class SqlEmployeeNumberAllocator : IEmployeeNumberAllocator
{
    private readonly AppDbContext _context;

    public SqlEmployeeNumberAllocator(AppDbContext context)
    {
        _context = context;
    }

    public async Task<string> AllocateAsync(int organizationId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            MERGE org.OrganizationEmployeeNumberSequences WITH (HOLDLOCK) AS target
            USING (SELECT @orgId AS OrganizationId) AS source
            ON target.OrganizationId = source.OrganizationId
            WHEN MATCHED THEN
                UPDATE SET LastAllocatedNumber = target.LastAllocatedNumber + 1
            WHEN NOT MATCHED THEN
                INSERT (OrganizationId, LastAllocatedNumber) VALUES (@orgId, 1)
            OUTPUT INSERTED.LastAllocatedNumber;
            """;

        await using var command = _context.Database.GetDbConnection().CreateCommand();
        command.CommandText = sql;
        command.Parameters.Add(new SqlParameter("@orgId", organizationId));

        if (command.Connection?.State != System.Data.ConnectionState.Open)
            await _context.Database.OpenConnectionAsync(cancellationToken);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        if (result is null or DBNull)
            throw new BusinessRuleException("Employee number allocation failed.");

        var number = Convert.ToInt32(result);
        return EmployeeNumberAllocator.FormatEmployeeNumber(number);
    }
}
