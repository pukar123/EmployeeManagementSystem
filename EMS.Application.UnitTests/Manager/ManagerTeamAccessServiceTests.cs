using EMS.Application.Services.Authorization;
using EMS.Application.Services.EmployeePortal;
using EMS.Application.Services.Manager;
using EMS.Domain.DbModels;
using EMS.Domain.Repositories.Interface;
using MockQueryable;
using Moq;
using Pukar.Shared;

namespace EMS.Application.UnitTests.Manager;

public class ManagerTeamAccessServiceTests
{
    [Test]
    public async Task ResolveEffectiveManagerIdAsync_ReturnsManagerId_ForAdmin()
    {
        var employees = new List<Employee>
        {
            new()
            {
                Id = 5,
                OrganizationId = 1,
                FirstName = "M",
                LastName = "Boss",
                EmployeeNumber = "EMP005",
                IsArchived = false,
            },
        };

        var sut = CreateSut(
            roleKeys: ["ADMIN"],
            employees: employees,
            menuId: 99,
            linkedEmployee: null,
            menuAllowed: false);

        var result = await sut.ResolveEffectiveManagerIdAsync(1, 5, CancellationToken.None);

        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void ResolveEffectiveManagerIdAsync_Throws_WhenAdminMissingManagerId()
    {
        var sut = CreateSut(roleKeys: ["ADMIN"]);

        var ex = Assert.ThrowsAsync<BusinessRuleException>(() =>
            sut.ResolveEffectiveManagerIdAsync(1, null, CancellationToken.None));

        Assert.That(ex!.Message, Is.EqualTo(ManagerTeamAccessMessages.Denied));
    }

    [Test]
    public void ResolveEffectiveManagerIdAsync_Throws_WhenNonAdminRequestsDifferentManager()
    {
        var linked = new Employee { Id = 10, OrganizationId = 1, IsArchived = false };
        var sut = CreateSut(
            roleKeys: ["MANAGER"],
            linkedEmployee: linked,
            menuAllowed: true,
            menuId: 42);

        var ex = Assert.ThrowsAsync<BusinessRuleException>(() =>
            sut.ResolveEffectiveManagerIdAsync(1, 99, CancellationToken.None));

        Assert.That(ex!.Message, Is.EqualTo(ManagerTeamAccessMessages.Denied));
    }

    [Test]
    public async Task ResolveEffectiveManagerIdAsync_ReturnsLinkedId_WhenManagerHasMenuAccess()
    {
        var linked = new Employee { Id = 10, OrganizationId = 1, IsArchived = false };
        var sut = CreateSut(
            roleKeys: ["MANAGER"],
            linkedEmployee: linked,
            menuAllowed: true,
            menuId: 42);

        var result = await sut.ResolveEffectiveManagerIdAsync(1, null, CancellationToken.None);

        Assert.That(result, Is.EqualTo(10));
    }

    [Test]
    public void ResolveEffectiveManagerIdAsync_Throws_WhenNoMenuAccess()
    {
        var linked = new Employee { Id = 10, OrganizationId = 1, IsArchived = false };
        var sut = CreateSut(
            roleKeys: ["MANAGER"],
            linkedEmployee: linked,
            menuAllowed: false,
            menuId: 42);

        var ex = Assert.ThrowsAsync<BusinessRuleException>(() =>
            sut.ResolveEffectiveManagerIdAsync(1, null, CancellationToken.None));

        Assert.That(ex!.Message, Is.EqualTo(ManagerTeamAccessMessages.Denied));
    }

    [Test]
    public void ResolveEffectiveManagerIdAsync_Throws_WhenNoLinkedEmployee()
    {
        var sut = CreateSut(
            roleKeys: ["MANAGER"],
            linkedEmployee: null,
            menuAllowed: true,
            menuId: 42);

        var ex = Assert.ThrowsAsync<BusinessRuleException>(() =>
            sut.ResolveEffectiveManagerIdAsync(1, null, CancellationToken.None));

        Assert.That(ex!.Message, Is.EqualTo(ManagerTeamAccessMessages.Denied));
    }

    [Test]
    public async Task IsDirectReportAsync_ReturnsTrue_WhenEmployeeReportsToManager()
    {
        var employees = new List<Employee>
        {
            new() { Id = 10, ManagerId = 5, IsArchived = false },
        };

        var sut = CreateSut(employees: employees);

        var result = await sut.IsDirectReportAsync(5, 10, CancellationToken.None);

        Assert.That(result, Is.True);
    }

    [Test]
    public async Task IsDirectReportAsync_ReturnsFalse_WhenEmployeeOutsideTree()
    {
        var employees = new List<Employee>
        {
            new() { Id = 10, ManagerId = 7, IsArchived = false },
        };

        var sut = CreateSut(employees: employees);

        var result = await sut.IsDirectReportAsync(5, 10, CancellationToken.None);

        Assert.That(result, Is.False);
    }

    private static ManagerTeamAccessService CreateSut(
        IReadOnlyList<string>? roleKeys = null,
        IReadOnlyList<Employee>? employees = null,
        Employee? linkedEmployee = null,
        bool menuAllowed = false,
        int menuId = 1)
    {
        var identity = new Mock<IIdentityContext>();
        identity.Setup(x => x.GetCurrent()).Returns(new UserIdentitySnapshot
        {
            RoleKeys = roleKeys?.ToList() ?? [],
        });

        var linked = new Mock<ILinkedEmployeeService>();
        linked.Setup(x => x.TryGetLinkedEmployeeAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(linkedEmployee);

        var permissions = new Mock<IPermissionEvaluator>();
        permissions
            .Setup(x => x.IsMenuAllowedAsync(It.IsAny<IReadOnlyList<string>>(), menuId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(menuAllowed);

        var menus = new Mock<IBaseRepository<Menu>>();
        menus.Setup(x => x.GetQueryable()).Returns(new List<Menu>
        {
            new() { Id = menuId, Key = "manager.team" },
        }.BuildMock());

        var employeeRepo = new Mock<IBaseRepository<Employee>>();
        employeeRepo.Setup(x => x.GetQueryable()).Returns((employees ?? []).BuildMock());

        return new ManagerTeamAccessService(
            identity.Object,
            linked.Object,
            permissions.Object,
            menus.Object,
            employeeRepo.Object);
    }
}
