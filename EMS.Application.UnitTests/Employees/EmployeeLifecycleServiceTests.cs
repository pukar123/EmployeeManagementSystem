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

public class EmployeeLifecycleServiceTests
{
    [Test]
    public async Task ArchiveAsync_DoesNotTerminateEmployment()
    {
        var employeeRepo = new InMemoryRepositoryMock<Employee>(e => e.Id, (e, id) => e.Id = id);
        employeeRepo.Seed(new Employee
        {
            Id = 1,
            OrganizationId = 1,
            Email = "a@example.com",
            EmploymentStatus = EmploymentStatus.Active,
            IsActive = true,
            IsArchived = false,
        });
        var employees = employeeRepo.CreateMock();

        var retentionRepo = new Mock<IBaseRepository<EmployeeRetentionPolicy>>();
        retentionRepo.Setup(x => x.GetQueryable()).Returns(new List<EmployeeRetentionPolicy>().BuildMock());

        var statusHistory = new InMemoryRepositoryMock<EmployeeEmploymentStatusHistory>(h => (int)h.Id, (h, id) => h.Id = id);
        var sut = BuildService(employees.Object, statusHistory.CreateMock().Object, retentionRepo.Object);

        var result = await sut.ArchiveAsync(1, new ArchiveEmployeeRequestModel { Reason = "Records cleanup" }, CancellationToken.None);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.EmploymentStatus, Is.EqualTo(EmploymentStatus.Active));
        Assert.That(result.IsArchived, Is.True);
    }

    [Test]
    public void TerminateAsync_RequiresReason()
    {
        var employeeRepo = new InMemoryRepositoryMock<Employee>(e => e.Id, (e, id) => e.Id = id);
        employeeRepo.Seed(new Employee
        {
            Id = 1,
            OrganizationId = 1,
            Email = "a@example.com",
            EmploymentStatus = EmploymentStatus.Active,
            IsArchived = false,
        });
        var employees = employeeRepo.CreateMock();

        var sut = BuildService(employees.Object);

        var ex = Assert.ThrowsAsync<BusinessRuleException>(() =>
            sut.TerminateAsync(1, new TerminateEmployeeRequestModel
            {
                EffectiveDateUtc = DateTime.UtcNow,
                Reason = "   ",
            }, CancellationToken.None));

        Assert.That(ex!.Message, Does.Contain("Reason").Or.Contain("required"));
    }

    [Test]
    public async Task RestoreAsync_BlockedAfterRetentionEnds()
    {
        var employeeRepo = new InMemoryRepositoryMock<Employee>(e => e.Id, (e, id) => e.Id = id);
        employeeRepo.Seed(new Employee
        {
            Id = 1,
            OrganizationId = 1,
            Email = "a@example.com",
            IsArchived = true,
            RetentionUntilUtc = DateTime.UtcNow.AddDays(-1),
        });
        var employees = employeeRepo.CreateMock();

        var sut = BuildService(employees.Object);

        var ex = Assert.ThrowsAsync<BusinessRuleException>(() => sut.RestoreAsync(1, CancellationToken.None));
        Assert.That(ex!.Message, Does.Contain("retention"));
    }

    private static EmployeeLifecycleService BuildService(
        IBaseRepository<Employee> employees,
        IBaseRepository<EmployeeEmploymentStatusHistory>? statusHistory = null,
        IBaseRepository<EmployeeRetentionPolicy>? retention = null)
    {
        statusHistory ??= Mock.Of<IBaseRepository<EmployeeEmploymentStatusHistory>>();
        retention ??= Mock.Of<IBaseRepository<EmployeeRetentionPolicy>>();

        var identity = new Mock<IIdentityContext>();
        identity.Setup(x => x.GetCurrent()).Returns(new UserIdentitySnapshot());

        return new EmployeeLifecycleService(
            employees,
            statusHistory,
            retention,
            identity.Object,
            Mock.Of<IEmployeeUserManagementGateway>(),
            CreateValidator(employees));
    }

    private static EmployeeRelationshipValidator CreateValidator(IBaseRepository<Employee> employees)
        => new(
            Mock.Of<IBaseRepository<Organization>>(),
            Mock.Of<IBaseRepository<Department>>(),
            Mock.Of<IBaseRepository<Location>>(),
            Mock.Of<IBaseRepository<JobPosition>>(),
            employees);
}
