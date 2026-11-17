using EMS.Application.DTOs.Department;
using EMS.Application.Services.Departments;
using EMS.Application.UnitTests.Infrastructure;
using EMS.Domain.DbModels;
using Microsoft.EntityFrameworkCore;
using Moq;
using Pukar.Shared;

namespace EMS.Application.UnitTests.Departments;

public class DepartmentServiceTests
{
    private static InMemoryRepositoryMock<Department> CreateRepository()
    {
        var repository = new InMemoryRepositoryMock<Department>(d => d.Id, (d, id) => d.Id = id);
        repository.Seed(
            new Department { Id = 1, OrganizationId = 10, Name = "Finance", Code = "FIN", IsActive = true },
            new Department { Id = 2, OrganizationId = 10, Name = "People", Code = "PPL", IsActive = true });
        return repository;
    }

    [Test]
    public void UpdateAsync_ThrowsBusinessRuleException_WhenSaveHitsConstraintConflict()
    {
        var repository = CreateRepository();
        var mock = repository.CreateMock();
        mock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException("Unique index violation"));
        var sut = new DepartmentService(mock.Object);

        var request = new UpdateDepartmentRequestModel { Name = "Finance and Payroll", Code = "FIN", IsActive = true };

        var ex = Assert.ThrowsAsync<BusinessRuleException>(() => sut.UpdateAsync(1, request, CancellationToken.None));

        Assert.That(ex!.Message, Is.EqualTo("Unable to update department due to a data constraint conflict."));
    }

    [Test]
    public void UpdateAsync_ThrowsDuplicateNameBusinessRuleException_WhenNameUsedByAnotherDepartment()
    {
        var repository = CreateRepository();
        var mock = repository.CreateMock();
        var sut = new DepartmentService(mock.Object);

        var request = new UpdateDepartmentRequestModel { Name = "  people ", Code = "FIN", IsActive = true };

        var ex = Assert.ThrowsAsync<BusinessRuleException>(() => sut.UpdateAsync(1, request, CancellationToken.None));

        Assert.That(ex!.Message, Is.EqualTo("A department with this name already exists in the organization."));
        mock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task UpdateAsync_SavesAndReturnsUpdatedDepartment_WhenNoConflict()
    {
        var repository = CreateRepository();
        var mock = repository.CreateMock();
        var sut = new DepartmentService(mock.Object);

        var request = new UpdateDepartmentRequestModel { Name = "Finance and Payroll", Code = "FIN", IsActive = true };

        var result = await sut.UpdateAsync(1, request, CancellationToken.None);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Name, Is.EqualTo("Finance and Payroll"));
        mock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
