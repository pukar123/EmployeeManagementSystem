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

public class EmployeeIdentityProvisioningServiceTests
{
    [Test]
    public void ProvisionAsync_Throws_WhenEmailAccountExistsWithoutExplicitLink()
    {
        var employeeRepo = new InMemoryRepositoryMock<Employee>(e => e.Id, (e, id) => e.Id = id);
        employeeRepo.Seed(new Employee
        {
            Id = 1,
            OrganizationId = 1,
            Email = "jane@example.com",
            FirstName = "Jane",
            LastName = "Doe",
            EmployeeNumber = "EMP001",
            IsActive = true,
        });
        var employees = employeeRepo.CreateMock();

        var orgRepo = new Mock<IBaseRepository<Organization>>();
        orgRepo.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Organization { Id = 1 });

        var gateway = new Mock<IEmployeeUserManagementGateway>();
        gateway.Setup(x => x.GetUserByEmailAsync("jane@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmployeeLinkedUserSnapshot { Id = 42, Email = "jane@example.com" });

        var validator = new EmployeeRelationshipValidator(
            orgRepo.Object,
            Mock.Of<IBaseRepository<Department>>(),
            Mock.Of<IBaseRepository<Location>>(),
            Mock.Of<IBaseRepository<JobPosition>>(),
            employees.Object);

        var sut = new EmployeeIdentityProvisioningService(employees.Object, orgRepo.Object, gateway.Object, validator);

        var ex = Assert.ThrowsAsync<BusinessRuleException>(() => sut.ProvisionAsync(1, CancellationToken.None));
        Assert.That(ex!.Message, Does.Contain("link-user"));
        gateway.Verify(x => x.CreateUserAsync(It.IsAny<CreateEmployeeLinkedUserRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task ProvisionAsync_UsesSecureTemporaryPassword_ForNewUsers()
    {
        var employeeRepo = new InMemoryRepositoryMock<Employee>(e => e.Id, (e, id) => e.Id = id);
        employeeRepo.Seed(new Employee
        {
            Id = 1,
            OrganizationId = 1,
            Email = "jane@example.com",
            FirstName = "Jane",
            LastName = "Doe",
            EmployeeNumber = "EMP001",
            IsActive = true,
        });
        var employees = employeeRepo.CreateMock();

        var orgRepo = new Mock<IBaseRepository<Organization>>();
        orgRepo.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Organization { Id = 1 });

        string? capturedPassword = null;
        var gateway = new Mock<IEmployeeUserManagementGateway>();
        gateway.Setup(x => x.GetUserByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EmployeeLinkedUserSnapshot?)null);
        gateway.Setup(x => x.CreateUserAsync(It.IsAny<CreateEmployeeLinkedUserRequest>(), It.IsAny<CancellationToken>()))
            .Callback<CreateEmployeeLinkedUserRequest, CancellationToken>((req, _) => capturedPassword = req.Password)
            .ReturnsAsync(new EmployeeLinkedUserSnapshot { Id = 9, Email = "jane@example.com" });
        gateway.Setup(x => x.GetRoleIdsForUserAsync(9, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<int>());

        var validator = new EmployeeRelationshipValidator(
            orgRepo.Object,
            Mock.Of<IBaseRepository<Department>>(),
            Mock.Of<IBaseRepository<Location>>(),
            Mock.Of<IBaseRepository<JobPosition>>(),
            employees.Object);

        var sut = new EmployeeIdentityProvisioningService(employees.Object, orgRepo.Object, gateway.Object, validator);

        var result = await sut.ProvisionAsync(1, CancellationToken.None);

        Assert.That(result.TemporaryPassword, Is.Not.Null.And.Length.GreaterThanOrEqualTo(16));
        Assert.That(capturedPassword, Is.EqualTo(result.TemporaryPassword));
        Assert.That(result.TemporaryPassword, Does.Not.Contain("@123"));
    }

    [Test]
    public void LinkExistingUserAsync_Throws_WhenEmailMismatch()
    {
        var employeeRepo = new InMemoryRepositoryMock<Employee>(e => e.Id, (e, id) => e.Id = id);
        employeeRepo.Seed(new Employee { Id = 1, OrganizationId = 1, Email = "jane@example.com" });
        var employees = employeeRepo.CreateMock();

        var gateway = new Mock<IEmployeeUserManagementGateway>();
        gateway.Setup(x => x.GetUserByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmployeeLinkedUserSnapshot { Id = 5, Email = "other@example.com" });

        var validator = new EmployeeRelationshipValidator(
            Mock.Of<IBaseRepository<Organization>>(),
            Mock.Of<IBaseRepository<Department>>(),
            Mock.Of<IBaseRepository<Location>>(),
            Mock.Of<IBaseRepository<JobPosition>>(),
            employees.Object);

        var sut = new EmployeeIdentityProvisioningService(employees.Object, Mock.Of<IBaseRepository<Organization>>(), gateway.Object, validator);

        var ex = Assert.ThrowsAsync<BusinessRuleException>(() =>
            sut.LinkExistingUserAsync(1, new LinkEmployeeUserRequestModel { UserId = 5 }, CancellationToken.None));
        Assert.That(ex!.Message, Does.Contain("email must match"));
    }
}
