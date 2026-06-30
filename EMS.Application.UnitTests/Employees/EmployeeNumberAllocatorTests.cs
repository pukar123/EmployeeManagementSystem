using EMS.Application.DTOs.Employee;
using EMS.Application.Services.Authorization;
using EMS.Application.Services.Employees;
using EMS.Application.UnitTests.Infrastructure;
using EMS.Domain.DbModels;
using EMS.Domain.Enums;
using EMS.Domain.Repositories.Interface;
using MockQueryable;
using Moq;
using Pukar.Shared;

namespace EMS.Application.UnitTests.Employees;

public class EmployeeNumberAllocatorTests
{
    [Test]
    public async Task AllocateAsync_ReturnsSequentialFormattedNumbers()
    {
        var repo = new InMemoryRepositoryMock<OrganizationEmployeeNumberSequence>(
            x => x.OrganizationId,
            (x, id) => x.OrganizationId = id);
        var sequences = repo.CreateMock();

        var sut = new EmployeeNumberAllocator(sequences.Object);

        var first = await sut.AllocateAsync(1, CancellationToken.None);
        var second = await sut.AllocateAsync(1, CancellationToken.None);

        Assert.That(first, Is.EqualTo("EMP001"));
        Assert.That(second, Is.EqualTo("EMP002"));
    }

    [Test]
    public void FormatEmployeeNumber_PreservesThreeDigitPadding()
    {
        Assert.That(EmployeeNumberAllocator.FormatEmployeeNumber(12), Is.EqualTo("EMP012"));
    }
}
