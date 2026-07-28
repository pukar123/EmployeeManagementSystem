using EMS.Application.DTOs.Employee;
using EMS.Application.Services.Employees;
using EMS.Domain.DbModels;
using EMS.Domain.Enums;
using EMS.Domain.Repositories.Interface;
using MockQueryable;
using Moq;
using Pukar.Shared;

namespace EMS.Application.UnitTests.Employees;

public class EmployeeRelationshipValidatorTests
{
    [Test]
    public void EnsureProfileDoesNotChangeOrganizationalFields_Throws_WhenDepartmentChanges()
    {
        var validator = new EmployeeRelationshipValidator(
            Mock.Of<IBaseRepository<Organization>>(),
            Mock.Of<IBaseRepository<Department>>(),
            Mock.Of<IBaseRepository<Location>>(),
            Mock.Of<IBaseRepository<JobPosition>>(),
            Mock.Of<IBaseRepository<Employee>>());

        var employee = new Employee { DepartmentId = 1, JobPositionId = 2, ManagerId = 3 };
        var request = new UpdateEmployeeRequestModel { DepartmentId = 9, JobPositionId = 2, ManagerId = 3 };

        var ex = Assert.Throws<BusinessRuleException>(() => validator.EnsureProfileDoesNotChangeOrganizationalFields(employee, request));
        Assert.That(ex!.Message, Does.Contain("transfer"));
    }

    [Test]
    public void NormalizeAndValidateProfile_Throws_ForInvalidEmploymentStatus()
    {
        var validator = new EmployeeRelationshipValidator(
            Mock.Of<IBaseRepository<Organization>>(),
            Mock.Of<IBaseRepository<Department>>(),
            Mock.Of<IBaseRepository<Location>>(),
            Mock.Of<IBaseRepository<JobPosition>>(),
            Mock.Of<IBaseRepository<Employee>>());

        var ex = Assert.Throws<BusinessRuleException>(() =>
            validator.NormalizeAndValidateProfile(
                "A",
                "B",
                "a@example.com",
                null,
                new DateTime(1990, 1, 1),
                new DateTime(2020, 1, 1),
                (EmploymentStatus)999));
        Assert.That(ex!.Message, Does.Contain("employment status"));
    }

    [Test]
    public void ValidateRelationships_Throws_WhenDepartmentCrossOrganization()
    {
        var deptRepo = new Mock<IBaseRepository<Department>>();
        deptRepo.Setup(x => x.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Department { Id = 5, OrganizationId = 2, IsActive = true });

        var orgRepo = new Mock<IBaseRepository<Organization>>();
        orgRepo.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Organization { Id = 1 });

        var employeeRepo = new Mock<IBaseRepository<Employee>>();
        employeeRepo.Setup(x => x.GetQueryable()).Returns(new List<Employee>().BuildMock());

        var validator = new EmployeeRelationshipValidator(
            orgRepo.Object,
            deptRepo.Object,
            Mock.Of<IBaseRepository<Location>>(),
            Mock.Of<IBaseRepository<JobPosition>>(),
            employeeRepo.Object);

        var ex = Assert.ThrowsAsync<BusinessRuleException>(() =>
            validator.ValidateRelationshipsForCreateOrUpdateAsync(
                1,
                1,
                5,
                null,
                null,
                null,
                "a@example.com",
                CancellationToken.None));
        Assert.That(ex!.Message, Does.Contain("same organization"));
    }

    [Test]
    public void ValidateRelationships_Throws_WhenCircularManager()
    {
        var employees = new List<Employee>
        {
            new() { Id = 1, OrganizationId = 1, ManagerId = null, IsActive = true, IsArchived = false, EmploymentStatus = EmploymentStatus.Active },
            new() { Id = 2, OrganizationId = 1, ManagerId = 1, IsActive = true, IsArchived = false, EmploymentStatus = EmploymentStatus.Active },
        };

        var employeeRepo = new Mock<IBaseRepository<Employee>>();
        employeeRepo.Setup(x => x.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(employees[1]);
        employeeRepo.Setup(x => x.GetQueryable()).Returns(employees.BuildMock());

        var orgRepo = new Mock<IBaseRepository<Organization>>();
        orgRepo.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new Organization { Id = 1 });

        var validator = new EmployeeRelationshipValidator(
            orgRepo.Object,
            Mock.Of<IBaseRepository<Department>>(),
            Mock.Of<IBaseRepository<Location>>(),
            Mock.Of<IBaseRepository<JobPosition>>(),
            employeeRepo.Object);

        var ex = Assert.ThrowsAsync<BusinessRuleException>(() =>
            validator.ValidateRelationshipsForCreateOrUpdateAsync(
                1,
                1,
                null,
                null,
                null,
                2,
                "a@example.com",
                CancellationToken.None));
        Assert.That(ex!.Message, Does.Contain("circular"));
    }
}
