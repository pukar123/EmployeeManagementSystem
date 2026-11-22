using EMS.Application.Services.Authorization;
using EMS.Application.Services.Leave;
using EMS.Domain.DbModels;
using EMS.Domain.Repositories.Interface;
using MockQueryable;
using Moq;

namespace EMS.Application.UnitTests.Leave;

/// <summary>B06 U1–U6: the single definition of "leave admin" (admin, or a role granted the leave.admin menu).</summary>
public class LeaveAdminAccessServiceTests
{
    private const int LeaveAdminMenuId = 77;
    private const int SelfServiceLeaveMenuId = 5;
    private const int LeaveAdminSettingsMenuId = 78;

    [Test]
    public async Task IsLeaveAdminAsync_ReturnsTrue_ForAdminRole_WithoutQueryingMenus()
    {
        var (sut, permissions, menus) = CreateSut(["ADMIN"]);

        var result = await sut.IsLeaveAdminAsync(CancellationToken.None);

        Assert.That(result, Is.True);
        menus.Verify(x => x.GetQueryable(), Times.Never);
        permissions.Verify(
            x => x.IsMenuAllowedAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task IsLeaveAdminAsync_ReturnsTrue_ForAdministratorRole()
    {
        var (sut, _, _) = CreateSut(["ADMINISTRATOR"]);

        Assert.That(await sut.IsLeaveAdminAsync(CancellationToken.None), Is.True);
    }

    [Test]
    public async Task IsLeaveAdminAsync_ReturnsTrue_ForNonAdminGrantedLeaveAdminMenu()
    {
        var (sut, _, _) = CreateSut(["LEAVEOFFICER"], leaveAdminAllowed: true);

        Assert.That(await sut.IsLeaveAdminAsync(CancellationToken.None), Is.True);
    }

    [Test]
    public async Task IsLeaveAdminAsync_ReturnsFalse_ForLeaveMenuOnly_AndNeverAsksAboutLeaveMenu()
    {
        // The "leave" and "leave.admin.settings" menus are allowed here, to prove they do not count.
        var (sut, permissions, _) = CreateSut(["LEAVEUSER"], leaveAdminAllowed: false);

        var result = await sut.IsLeaveAdminAsync(CancellationToken.None);

        Assert.That(result, Is.False);
        permissions.Verify(
            x => x.IsMenuAllowedAsync(It.IsAny<IReadOnlyList<string>>(), LeaveAdminMenuId, It.IsAny<CancellationToken>()),
            Times.Once);
        permissions.Verify(
            x => x.IsMenuAllowedAsync(It.IsAny<IReadOnlyList<string>>(), SelfServiceLeaveMenuId, It.IsAny<CancellationToken>()),
            Times.Never);
        permissions.Verify(
            x => x.IsMenuAllowedAsync(It.IsAny<IReadOnlyList<string>>(), LeaveAdminSettingsMenuId, It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task IsLeaveAdminAsync_WhenLeaveAdminMenuIsMissing_ReturnsFalseForNonAdminAndTrueForAdmin()
    {
        var (nonAdmin, _, _) = CreateSut(["LEAVEOFFICER"], leaveAdminAllowed: true, includeLeaveAdminMenu: false);
        var (admin, _, _) = CreateSut(["ADMIN"], includeLeaveAdminMenu: false);

        var nonAdminResult = await nonAdmin.IsLeaveAdminAsync(CancellationToken.None);
        var adminResult = await admin.IsLeaveAdminAsync(CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(nonAdminResult, Is.False);
            Assert.That(adminResult, Is.True);
        });
    }

    [Test]
    public async Task IsLeaveAdminAsync_ReturnsFalse_WithNoRoleKeys()
    {
        var (sut, _, _) = CreateSut([], leaveAdminAllowed: true);

        Assert.That(await sut.IsLeaveAdminAsync(CancellationToken.None), Is.False);
    }

    internal static (LeaveAdminAccessService Sut, Mock<IPermissionEvaluator> Permissions, Mock<IBaseRepository<Menu>> Menus) CreateSut(
        string[] roleKeys,
        bool leaveAdminAllowed = false,
        bool includeLeaveAdminMenu = true)
    {
        var identity = new Mock<IIdentityContext>();
        identity.Setup(x => x.GetCurrent()).Returns(new UserIdentitySnapshot { RoleKeys = roleKeys.ToList() });

        var permissions = new Mock<IPermissionEvaluator>();
        permissions
            .Setup(x => x.IsMenuAllowedAsync(It.IsAny<IReadOnlyList<string>>(), LeaveAdminMenuId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(leaveAdminAllowed);
        permissions
            .Setup(x => x.IsMenuAllowedAsync(It.IsAny<IReadOnlyList<string>>(), SelfServiceLeaveMenuId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        permissions
            .Setup(x => x.IsMenuAllowedAsync(It.IsAny<IReadOnlyList<string>>(), LeaveAdminSettingsMenuId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var menuRows = new List<Menu>
        {
            new() { Id = SelfServiceLeaveMenuId, Key = "leave" },
            new() { Id = LeaveAdminSettingsMenuId, Key = "leave.admin.settings" },
        };
        if (includeLeaveAdminMenu)
            menuRows.Add(new Menu { Id = LeaveAdminMenuId, Key = "leave.admin" });

        var menus = new Mock<IBaseRepository<Menu>>();
        menus.Setup(x => x.GetQueryable()).Returns(() => menuRows.BuildMock());

        return (new LeaveAdminAccessService(identity.Object, permissions.Object, menus.Object), permissions, menus);
    }
}
