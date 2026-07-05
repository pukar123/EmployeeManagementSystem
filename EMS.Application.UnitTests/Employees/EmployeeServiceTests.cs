using EMS.Application.DTOs.Employee;
using EMS.Application.Services.Authorization;
using EMS.Application.Services.Integrations;
using EMS.Application.Services.Employees;
using EMS.Application.UnitTests.Infrastructure;
using EMS.Domain.DbModels;
using EMS.Domain.Enums;
using EMS.Domain.Repositories.Interface;
using MockQueryable;
using Moq;
using Pukar.Shared;

namespace EMS.Application.UnitTests.Employees;

public class EmployeeServiceTests
{
    private static EmployeeRelationshipValidator CreateValidator(
        IBaseRepository<Organization>? organizations = null,
        IBaseRepository<Department>? departments = null,
        IBaseRepository<Location>? locations = null,
        IBaseRepository<JobPosition>? jobPositions = null,
        IBaseRepository<Employee>? employees = null)
    {
        organizations ??= Mock.Of<IBaseRepository<Organization>>();
        departments ??= Mock.Of<IBaseRepository<Department>>();
        locations ??= Mock.Of<IBaseRepository<Location>>();
        jobPositions ??= Mock.Of<IBaseRepository<JobPosition>>();
        employees ??= Mock.Of<IBaseRepository<Employee>>();

        return new EmployeeRelationshipValidator(
            organizations,
            departments,
            locations,
            jobPositions,
            employees);
    }

    [Test]
    public void UpdateAsync_Throws_WhenSelfManager()
    {
        var employeeRepo = new InMemoryRepositoryMock<Employee>(e => e.Id, (e, id) => e.Id = id);
        employeeRepo.Seed(new Employee
        {
            Id = 1,
            OrganizationId = 1,
            Email = "a@example.com",
            FirstName = "A",
            LastName = "B",
            ManagerId = null,
            DateOfBirth = new DateTime(1990, 1, 1),
            DateJoined = new DateTime(2020, 1, 1),
            EmploymentStatus = EmploymentStatus.Active,
        });
        var employees = employeeRepo.CreateMock();

        var orgRepo = new Mock<IBaseRepository<Organization>>();
        orgRepo.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Organization { Id = 1 });
        employees.Setup(x => x.GetQueryable()).Returns(employeeRepo.Items.BuildMock());

        var validator = CreateValidator(orgRepo.Object, employees: employees.Object);
        var sut = BuildService(employees.Object, validator);

        var update = ValidUpdateRequest();
        update.ManagerId = 1;

        var ex = Assert.ThrowsAsync<BusinessRuleException>(() =>
            sut.UpdateAsync(1, update, CancellationToken.None));
        Assert.That(ex!.Message, Does.Contain("transfer"));
    }

    [Test]
    public void CreateAsync_Throws_WhenCrossOrganizationDepartment()
    {
        var employeeRepo = new InMemoryRepositoryMock<Employee>(e => e.Id, (e, id) => e.Id = id);
        var employees = employeeRepo.CreateMock();

        var orgRepo = new Mock<IBaseRepository<Organization>>();
        orgRepo.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Organization { Id = 1 });

        var deptRepo = new Mock<IBaseRepository<Department>>();
        deptRepo.Setup(x => x.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Department { Id = 5, OrganizationId = 2, IsActive = true });

        employees.Setup(x => x.GetQueryable()).Returns(employeeRepo.Items.BuildMock());

        var validator = CreateValidator(orgRepo.Object, departments: deptRepo.Object, employees: employees.Object);
        var sut = BuildService(employees.Object, validator);

        var request = ValidCreateRequest();
        request.DepartmentId = 5;

        var ex = Assert.ThrowsAsync<BusinessRuleException>(() => sut.CreateAsync(request, CancellationToken.None));
        Assert.That(ex!.Message, Does.Contain("same organization"));
    }

    [Test]
    public void CreateAsync_Throws_WhenDuplicateEmail()
    {
        var employeeRepo = new InMemoryRepositoryMock<Employee>(e => e.Id, (e, id) => e.Id = id);
        employeeRepo.Seed(new Employee { Id = 2, OrganizationId = 1, Email = "dup@example.com" });
        var employees = employeeRepo.CreateMock();

        var orgRepo = new Mock<IBaseRepository<Organization>>();
        orgRepo.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Organization { Id = 1 });
        employees.Setup(x => x.GetQueryable()).Returns(employeeRepo.Items.BuildMock());

        var validator = CreateValidator(orgRepo.Object, employees: employees.Object);
        var sut = BuildService(employees.Object, validator);

        var request = ValidCreateRequest();
        request.Email = "dup@example.com";

        var ex = Assert.ThrowsAsync<BusinessRuleException>(() => sut.CreateAsync(request, CancellationToken.None));
        Assert.That(ex!.Message, Does.Contain("already exists"));
    }

    [Test]
    public void CreateAsync_Throws_WhenInvalidEmploymentStatus()
    {
        var employeeRepo = new InMemoryRepositoryMock<Employee>(e => e.Id, (e, id) => e.Id = id);
        var employees = employeeRepo.CreateMock();

        var validator = CreateValidator(employees: employees.Object);
        var sut = BuildService(employees.Object, validator);

        var request = ValidCreateRequest();
        request.EmploymentStatus = (EmploymentStatus)999;

        var ex = Assert.ThrowsAsync<BusinessRuleException>(() => sut.CreateAsync(request, CancellationToken.None));
        Assert.That(ex!.Message, Does.Contain("employment status"));
    }

    [Test]
    public void CreateAsync_Throws_WhenDateJoinedBeforeDateOfBirth()
    {
        var employeeRepo = new InMemoryRepositoryMock<Employee>(e => e.Id, (e, id) => e.Id = id);
        var employees = employeeRepo.CreateMock();
        var orgRepo = new Mock<IBaseRepository<Organization>>();
        orgRepo.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Organization { Id = 1 });
        employees.Setup(x => x.GetQueryable()).Returns(employeeRepo.Items.BuildMock());

        var validator = CreateValidator(orgRepo.Object, employees: employees.Object);
        var sut = BuildService(employees.Object, validator);

        var request = ValidCreateRequest();
        request.DateOfBirth = new DateTime(2000, 1, 1);
        request.DateJoined = new DateTime(1999, 1, 1);

        var ex = Assert.ThrowsAsync<BusinessRuleException>(() => sut.CreateAsync(request, CancellationToken.None));
        Assert.That(ex!.Message, Does.Contain("Date joined"));
    }

    [Test]
    public void UpdateAsync_Throws_WhenArchived()
    {
        var employeeRepo = new InMemoryRepositoryMock<Employee>(e => e.Id, (e, id) => e.Id = id);
        employeeRepo.Seed(new Employee
        {
            Id = 1,
            OrganizationId = 1,
            Email = "a@example.com",
            IsArchived = true,
            FirstName = "A",
            LastName = "B",
        });
        var employees = employeeRepo.CreateMock();

        var validator = CreateValidator(employees: employees.Object);
        var sut = BuildService(employees.Object, validator);

        var ex = Assert.ThrowsAsync<BusinessRuleException>(() =>
            sut.UpdateAsync(1, ValidUpdateRequest(), CancellationToken.None));
        Assert.That(ex!.Message, Does.Contain("Archived"));
    }

    [Test]
    public void UpdateAsync_Throws_WhenOrganizationalFieldsChange()
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
            DateOfBirth = new DateTime(1990, 1, 1),
            DateJoined = new DateTime(2020, 1, 1),
            EmploymentStatus = EmploymentStatus.Active,
        });
        var employees = employeeRepo.CreateMock();

        var orgRepo = new Mock<IBaseRepository<Organization>>();
        orgRepo.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Organization { Id = 1 });
        employees.Setup(x => x.GetQueryable()).Returns(employeeRepo.Items.BuildMock());

        var validator = CreateValidator(orgRepo.Object, employees: employees.Object);
        var sut = BuildService(employees.Object, validator);

        var update = ValidUpdateRequest();
        update.DepartmentId = 2;

        var ex = Assert.ThrowsAsync<BusinessRuleException>(() =>
            sut.UpdateAsync(1, update, CancellationToken.None));
        Assert.That(ex!.Message, Does.Contain("transfer"));
    }

    [Test]
    public async Task DeleteAsync_IsIdempotent_WhenAlreadyArchived()
    {
        var employeeRepo = new InMemoryRepositoryMock<Employee>(e => e.Id, (e, id) => e.Id = id);
        employeeRepo.Seed(new Employee { Id = 1, OrganizationId = 1, IsArchived = true, Email = "a@example.com" });
        var employees = employeeRepo.CreateMock();

        var gateway = new Mock<IEmployeeUserManagementGateway>();
        var validator = CreateValidator(employees: employees.Object);
        var sut = BuildService(employees.Object, validator, gateway.Object);

        var result = await sut.DeleteAsync(1, null, CancellationToken.None);

        Assert.That(result, Is.True);
        gateway.Verify(
            x => x.DeactivateLinkedUserAsync(It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task DeleteAsync_RevokesLinkedIdentity()
    {
        var employeeRepo = new InMemoryRepositoryMock<Employee>(e => e.Id, (e, id) => e.Id = id);
        employeeRepo.Seed(new Employee
        {
            Id = 1,
            OrganizationId = 1,
            Email = "a@example.com",
            ExternalIdentityKey = "7",
            IsArchived = false,
            EmploymentStatus = EmploymentStatus.Active,
        });
        var employees = employeeRepo.CreateMock();

        var retentionRepo = new Mock<IBaseRepository<EmployeeRetentionPolicy>>();
        retentionRepo.Setup(x => x.GetQueryable()).Returns(new List<EmployeeRetentionPolicy>().BuildMock());

        var gateway = new Mock<IEmployeeUserManagementGateway>();
        gateway.Setup(x => x.GetUserByIdAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmployeeLinkedUserSnapshot { Id = 7, Email = "a@example.com" });
        var validator = CreateValidator(employees: employees.Object);
        var sut = BuildService(employees.Object, validator, gateway.Object, retentionRepo.Object);

        var result = await sut.DeleteAsync(1, null, CancellationToken.None);

        Assert.That(result, Is.True);
        gateway.Verify(
            x => x.RevokeOperationalAccessAsync(7, It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Once);
        gateway.Verify(
            x => x.DeactivateLinkedUserAsync(7, It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Once);
        gateway.Verify(
            x => x.RevokeSessionsAsync(7, It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static CreateEmployeeRequestModel ValidCreateRequest() => new()
    {
        OrganizationId = 1,
        FirstName = "Jane",
        LastName = "Doe",
        Email = "jane@example.com",
        DateOfBirth = new DateTime(1990, 5, 1),
        DateJoined = new DateTime(2020, 1, 1),
        EmploymentStatus = EmploymentStatus.Active,
    };

    private static UpdateEmployeeRequestModel ValidUpdateRequest() => new()
    {
        OrganizationId = 1,
        DepartmentId = 1,
        FirstName = "Jane",
        LastName = "Doe",
        Email = "jane@example.com",
        DateOfBirth = new DateTime(1990, 5, 1),
        DateJoined = new DateTime(2020, 1, 1),
        EmploymentStatus = EmploymentStatus.Active,
    };

    private static EmployeeService BuildService(
        IBaseRepository<Employee> employees,
        EmployeeRelationshipValidator validator,
        IEmployeeUserManagementGateway? gateway = null,
        IBaseRepository<EmployeeRetentionPolicy>? retention = null)
    {
        gateway ??= Mock.Of<IEmployeeUserManagementGateway>();
        retention ??= Mock.Of<IBaseRepository<EmployeeRetentionPolicy>>();

        var positionHistory = Mock.Of<IBaseRepository<EmployeePositionHistory>>();
        var departmentHistory = Mock.Of<IBaseRepository<EmployeeDepartmentHistory>>();
        var managerHistory = Mock.Of<IBaseRepository<EmployeeManagerHistory>>();
        var identity = new Mock<IIdentityContext>();
        identity.Setup(x => x.GetCurrent()).Returns(new UserIdentitySnapshot());
        var roleSync = new Mock<IEmployeeRoleSyncService>();
        var allocator = new Mock<IEmployeeNumberAllocator>();
        allocator.Setup(x => x.AllocateAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync("EMP001");

        return new EmployeeService(
            employees,
            positionHistory,
            departmentHistory,
            managerHistory,
            Mock.Of<IBaseRepository<EmployeeEmploymentStatusHistory>>(),
            retention,
            Mock.Of<IBaseRepository<JobPosition>>(),
            Mock.Of<IBaseRepository<Department>>(),
            identity.Object,
            roleSync.Object,
            allocator.Object,
            gateway,
            Mock.Of<IIntegrationOutboxWriter>(),
            validator);
    }
}
