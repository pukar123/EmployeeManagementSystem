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

public class EmployeeTransferServiceTests
{
    [Test]
    public async Task TransferDepartmentAsync_ClosesPreviousHistoryEffectiveToUtc()
    {
        var employeeRepo = new InMemoryRepositoryMock<Employee>(e => e.Id, (e, id) => e.Id = id);
        employeeRepo.Seed(new Employee
        {
            Id = 1,
            OrganizationId = 1,
            DepartmentId = 1,
            Email = "a@example.com",
            FirstName = "A",
            LastName = "B",
            EmploymentStatus = EmploymentStatus.Active,
        });
        var employees = employeeRepo.CreateMock();

        var deptHistoryRepo = new InMemoryRepositoryMock<EmployeeDepartmentHistory>(h => (int)h.Id, (h, id) => h.Id = id);
        deptHistoryRepo.Seed(new EmployeeDepartmentHistory
        {
            Id = 10,
            EmployeeId = 1,
            PreviousDepartmentId = null,
            NewDepartmentId = 1,
            EffectiveFromUtc = new DateTime(2024, 1, 1),
            EffectiveToUtc = null,
            CreatedAtUtc = new DateTime(2024, 1, 1),
        });
        var departmentHistory = deptHistoryRepo.CreateMock();

        var deptRepo = new Mock<IBaseRepository<Department>>();
        deptRepo.Setup(x => x.GetByIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Department { Id = 2, OrganizationId = 1, IsActive = true });

        var orgRepo = new Mock<IBaseRepository<Organization>>();
        orgRepo.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Organization { Id = 1 });

        employees.Setup(x => x.GetQueryable()).Returns(employeeRepo.Items.BuildMock());

        var validator = new EmployeeRelationshipValidator(
            orgRepo.Object,
            deptRepo.Object,
            Mock.Of<IBaseRepository<Location>>(),
            Mock.Of<IBaseRepository<JobPosition>>(),
            employees.Object);

        var identity = new Mock<IIdentityContext>();
        identity.Setup(x => x.GetCurrent()).Returns(new UserIdentitySnapshot());

        var sut = new EmployeeTransferService(
            employees.Object,
            Mock.Of<IBaseRepository<EmployeePositionHistory>>(),
            departmentHistory.Object,
            Mock.Of<IBaseRepository<EmployeeManagerHistory>>(),
            identity.Object,
            Mock.Of<IEmployeeRoleSyncService>(),
            validator,
            Mock.Of<IEmployeeBusinessDateHelper>());

        var effectiveFrom = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        await sut.TransferDepartmentAsync(
            1,
            new TransferEmployeeDepartmentRequestModel
            {
                NewDepartmentId = 2,
                EffectiveFromUtc = effectiveFrom,
                Reason = "Reorg",
            },
            CancellationToken.None);

        Assert.That(deptHistoryRepo.Items[0].EffectiveToUtc, Is.EqualTo(effectiveFrom));
        Assert.That(employeeRepo.Items[0].DepartmentId, Is.EqualTo(2));
        Assert.That(deptHistoryRepo.Items.Count, Is.EqualTo(2));
    }

    [Test]
    public void TransferDepartmentAsync_Throws_WhenNoChange()
    {
        var employeeRepo = new InMemoryRepositoryMock<Employee>(e => e.Id, (e, id) => e.Id = id);
        employeeRepo.Seed(new Employee { Id = 1, OrganizationId = 1, DepartmentId = 2, Email = "a@example.com" });
        var employees = employeeRepo.CreateMock();

        var validator = new EmployeeRelationshipValidator(
            Mock.Of<IBaseRepository<Organization>>(),
            Mock.Of<IBaseRepository<Department>>(),
            Mock.Of<IBaseRepository<Location>>(),
            Mock.Of<IBaseRepository<JobPosition>>(),
            employees.Object);

        var sut = new EmployeeTransferService(
            employees.Object,
            Mock.Of<IBaseRepository<EmployeePositionHistory>>(),
            Mock.Of<IBaseRepository<EmployeeDepartmentHistory>>(),
            Mock.Of<IBaseRepository<EmployeeManagerHistory>>(),
            Mock.Of<IIdentityContext>(),
            Mock.Of<IEmployeeRoleSyncService>(),
            validator,
            Mock.Of<IEmployeeBusinessDateHelper>());

        var ex = Assert.ThrowsAsync<BusinessRuleException>(() =>
            sut.TransferDepartmentAsync(
                1,
                new TransferEmployeeDepartmentRequestModel { NewDepartmentId = 2, EffectiveFromUtc = DateTime.UtcNow },
                CancellationToken.None));
        Assert.That(ex!.Message, Does.Contain("does not change"));
    }
}
