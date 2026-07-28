using EMS.Application.Services.Authorization;
using EMS.Application.Services.EmployeePortal;
using EMS.Application.Services.Employees;
using EMS.Application.Services.Manager;
using Moq;
using Pukar.Shared;

namespace EMS.Application.UnitTests.Employees;

public class EmployeeAccessServiceTests
{
    [Test]
    public void EnsureCanViewEmployeesAsync_Throws_WhenNoPermission()
    {
        var (sut, _, _) = CreateSut(roleKeys: ["HR"], viewGranted: false);

        var ex = Assert.ThrowsAsync<BusinessRuleException>(() => sut.EnsureCanViewEmployeesAsync(CancellationToken.None));
        Assert.That(ex!.Message, Is.EqualTo(EmployeeAccessMessages.Denied));
    }

    [Test]
    public async Task EnsureCanViewEmployeesAsync_Allows_WhenViewCapabilityGranted()
    {
        var (sut, _, _) = CreateSut(roleKeys: ["HR"], viewGranted: true);

        Assert.DoesNotThrowAsync(() => sut.EnsureCanViewEmployeesAsync(CancellationToken.None));
    }

    [Test]
    public void EnsureCanManageEmployeesAsync_Throws_WhenOnlyViewGranted()
    {
        var (sut, _, _) = CreateSut(roleKeys: ["HR"], viewGranted: true, manageGranted: false);

        var ex = Assert.ThrowsAsync<BusinessRuleException>(() => sut.EnsureCanManageEmployeesAsync(CancellationToken.None));
        Assert.That(ex!.Message, Is.EqualTo(EmployeeAccessMessages.Denied));
    }

    [Test]
    public async Task EnsureCanManageEmployeesAsync_Allows_AdminBypass()
    {
        var (sut, _, _) = CreateSut(roleKeys: ["ADMIN"]);

        Assert.DoesNotThrowAsync(() => sut.EnsureCanManageEmployeesAsync(CancellationToken.None));
    }

    [Test]
    public async Task GetMyCapabilitiesAsync_ReturnsAllForAdmin()
    {
        var (sut, _, _) = CreateSut(roleKeys: ["ADMIN"]);

        var caps = await sut.GetMyCapabilitiesAsync(CancellationToken.None);

        Assert.That(caps.View, Is.True);
        Assert.That(caps.Manage, Is.True);
        Assert.That(caps.Access, Is.True);
        Assert.That(caps.Export, Is.True);
    }

    [Test]
    public async Task EnsureCanViewEmployeeProfileAsync_AllowsDirectReport_WhenTeamMenuGranted()
    {
        var (sut, managerTeamAccess, _) = CreateSut(roleKeys: ["MANAGER"], viewGranted: false);
        managerTeamAccess.Setup(x => x.CanViewTeamDashboardAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        managerTeamAccess.Setup(x => x.IsDirectReportAsync(10, 20, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        Assert.DoesNotThrowAsync(() => sut.EnsureCanViewEmployeeProfileAsync(20, CancellationToken.None));
    }

    [Test]
    public void EnsureCanViewEmployeeProfileAsync_Throws_WhenOutsideReportingTree()
    {
        var (sut, managerTeamAccess, _) = CreateSut(roleKeys: ["MANAGER"], viewGranted: false);
        managerTeamAccess.Setup(x => x.CanViewTeamDashboardAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        managerTeamAccess.Setup(x => x.IsDirectReportAsync(10, 99, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var ex = Assert.ThrowsAsync<BusinessRuleException>(() =>
            sut.EnsureCanViewEmployeeProfileAsync(99, CancellationToken.None));

        Assert.That(ex!.Message, Is.EqualTo(EmployeeAccessMessages.Denied));
    }

    private static (EmployeeAccessService Sut, Mock<IManagerTeamAccessService> ManagerTeamAccess, Mock<ILinkedEmployeeService> Linked) CreateSut(
        IReadOnlyList<string> roleKeys,
        bool viewGranted = false,
        bool manageGranted = false)
    {
        var identity = new Mock<IIdentityContext>();
        identity.Setup(x => x.GetCurrent()).Returns(new UserIdentitySnapshot { RoleKeys = roleKeys.ToList() });

        var permissions = new Mock<IPermissionEvaluator>();
        permissions
            .Setup(x => x.HasCapabilityAsync(It.IsAny<IReadOnlyList<string>>(), EmployeeCapabilities.View, It.IsAny<CancellationToken>()))
            .ReturnsAsync(viewGranted);
        permissions
            .Setup(x => x.HasCapabilityAsync(It.IsAny<IReadOnlyList<string>>(), EmployeeCapabilities.Manage, It.IsAny<CancellationToken>()))
            .ReturnsAsync(manageGranted);

        var managerTeamAccess = new Mock<IManagerTeamAccessService>();
        var linked = new Mock<ILinkedEmployeeService>();
        linked.Setup(x => x.TryGetLinkedEmployeeAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EMS.Domain.DbModels.Employee { Id = 10, OrganizationId = 1, IsArchived = false });

        var sut = new EmployeeAccessService(
            identity.Object,
            permissions.Object,
            managerTeamAccess.Object,
            linked.Object);

        return (sut, managerTeamAccess, linked);
    }
}
