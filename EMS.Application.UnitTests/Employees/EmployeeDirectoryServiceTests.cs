using EMS.Application.DTOs.Employee;
using EMS.Application.Services.Employees;
using EMS.Application.UnitTests.Infrastructure;
using EMS.Domain.DbModels;
using EMS.Domain.Enums;
using EMS.Domain.Repositories.Interface;
using MockQueryable;
using Moq;
using Pukar.Shared;

namespace EMS.Application.UnitTests.Employees;

public class EmployeeDirectoryServiceTests
{
    [Test]
    public async Task QueryAsync_FiltersBySearchTrimAndCase()
    {
        var repo = new InMemoryRepositoryMock<Employee>(e => e.Id, (e, id) => e.Id = id);
        repo.Seed(
            new Employee
            {
                Id = 1,
                OrganizationId = 1,
                EmployeeNumber = "EMP001",
                FirstName = "Alice",
                LastName = "Smith",
                Email = "alice@example.com",
                EmploymentStatus = EmploymentStatus.Active,
                IsArchived = false,
            },
            new Employee
            {
                Id = 2,
                OrganizationId = 1,
                EmployeeNumber = "EMP002",
                FirstName = "Bob",
                LastName = "Jones",
                Email = "bob@example.com",
                EmploymentStatus = EmploymentStatus.Active,
                IsArchived = false,
            });
        var employees = repo.CreateMock();
        employees.Setup(x => x.GetQueryable()).Returns(repo.Items.BuildMock());

        var sut = new EmployeeDirectoryService(employees.Object, Mock.Of<IEmployeeUserManagementGateway>());
        var result = await sut.QueryAsync(new EmployeeDirectoryQueryModel
        {
            OrganizationId = 1,
            Search = "  ALICE  ",
            Page = 1,
            PageSize = 25,
        });

        Assert.That(result.TotalCount, Is.EqualTo(1));
        Assert.That(result.Items[0].FirstName, Is.EqualTo("Alice"));
    }

    [Test]
    public async Task QueryAsync_ExcludesArchivedByDefault()
    {
        var repo = new InMemoryRepositoryMock<Employee>(e => e.Id, (e, id) => e.Id = id);
        repo.Seed(
            new Employee { Id = 1, OrganizationId = 1, FirstName = "A", LastName = "B", Email = "a@x.com", IsArchived = false, EmploymentStatus = EmploymentStatus.Active },
            new Employee { Id = 2, OrganizationId = 1, FirstName = "C", LastName = "D", Email = "c@x.com", IsArchived = true, EmploymentStatus = EmploymentStatus.Terminated });
        var employees = repo.CreateMock();
        employees.Setup(x => x.GetQueryable()).Returns(repo.Items.BuildMock());

        var sut = new EmployeeDirectoryService(employees.Object, Mock.Of<IEmployeeUserManagementGateway>());
        var active = await sut.QueryAsync(new EmployeeDirectoryQueryModel { OrganizationId = 1, Page = 1, PageSize = 25 });
        var archived = await sut.QueryAsync(new EmployeeDirectoryQueryModel { OrganizationId = 1, Page = 1, PageSize = 25, IsArchived = true });

        Assert.That(active.TotalCount, Is.EqualTo(1));
        Assert.That(archived.TotalCount, Is.EqualTo(1));
        Assert.That(archived.Items[0].IsArchived, Is.True);
    }
}
