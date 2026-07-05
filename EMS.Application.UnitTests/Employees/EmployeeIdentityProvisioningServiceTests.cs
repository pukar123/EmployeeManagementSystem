using EMS.Application.DTOs.Employee;
using EMS.Application.Services.Employees;
using EMS.Application.UnitTests.Infrastructure;
using EMS.Domain.DbModels;
using EMS.Domain.Repositories.Interface;
using Moq;
using Pukar.Shared;

namespace EMS.Application.UnitTests.Employees;

public class EmployeeIdentityProvisioningServiceTests
{
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

        var sut = new EmployeeIdentityProvisioningService(employees.Object, gateway.Object, validator);

        var ex = Assert.ThrowsAsync<BusinessRuleException>(() =>
            sut.LinkExistingUserAsync(1, new LinkEmployeeUserRequestModel { UserId = 5 }, CancellationToken.None));
        Assert.That(ex!.Message, Does.Contain("email must match"));
    }

    [Test]
    public async Task LinkExistingUserAsync_SetsExternalIdentityKey_AndReturnsRoleKeys()
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
        });
        var employees = employeeRepo.CreateMock();

        var gateway = new Mock<IEmployeeUserManagementGateway>();
        gateway.Setup(x => x.GetUserByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmployeeLinkedUserSnapshot { Id = 5, Email = "jane@example.com" });
        gateway.Setup(x => x.GetRoleKeysForUserAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { "ADMIN" });

        var validator = new EmployeeRelationshipValidator(
            Mock.Of<IBaseRepository<Organization>>(),
            Mock.Of<IBaseRepository<Department>>(),
            Mock.Of<IBaseRepository<Location>>(),
            Mock.Of<IBaseRepository<JobPosition>>(),
            employees.Object);

        var sut = new EmployeeIdentityProvisioningService(employees.Object, gateway.Object, validator);

        var result = await sut.LinkExistingUserAsync(1, new LinkEmployeeUserRequestModel { UserId = 5 }, CancellationToken.None);

        Assert.That(result.AssignedRoleKeys, Is.EquivalentTo(new[] { "ADMIN" }));
        Assert.That(result.IsNewUser, Is.False);
        var employee = await employees.Object.GetByIdAsync(1);
        Assert.That(employee!.ExternalIdentityKey, Is.EqualTo("5"));
    }
}
