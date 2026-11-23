using EMS.Application.Services.Attendance;
using EMS.Application.Services.Authorization;
using EMS.Domain.DbModels;
using EMS.Domain.Repositories.Interface;
using MockQueryable;
using Moq;

namespace EMS.Application.UnitTests.Attendance;

/// <summary>B07: the single definition of "attendance admin" (admin, or a role granted the attendance.admin menu).</summary>
public class AttendanceAdminAccessServiceTests
{
    private const int AttendanceAdminMenuId = 88;
    private const int AttendancePageMenuId = 30;
    private const int AttendanceReportsMenuId = 31;
    private const int AttendanceAnalyticsMenuId = 32;

    [TestCase("ADMIN")]
    [TestCase("ADMINISTRATOR")]
    public async Task IsAttendanceAdminAsync_ReturnsTrue_ForAdminRoles_WithoutAskingTheEvaluator(string roleKey)
    {
        var (sut, permissions, menus) = CreateSut([roleKey]);

        var result = await sut.IsAttendanceAdminAsync(CancellationToken.None);

        Assert.That(result, Is.True);
        menus.Verify(x => x.GetQueryable(), Times.Never);
        permissions.Verify(
            x => x.IsMenuAllowedAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task IsAttendanceAdminAsync_ReturnsTrue_ForRoleGrantedAttendanceAdminMenu()
    {
        var (sut, _, _) = CreateSut(["ATTENDANCEOFFICER"], attendanceAdminAllowed: true);

        Assert.That(await sut.IsAttendanceAdminAsync(CancellationToken.None), Is.True);
    }

    [TestCase("ATTENDANCEUSER")]
    [TestCase("ATTENDANCEREPORTSONLY")]
    public async Task IsAttendanceAdminAsync_ReturnsFalse_ForPageOrReportsMenusOnly(string roleKey)
    {
        // The attendance, attendance.reports and attendance.analytics menus are allowed here, to prove they do not count.
        var (sut, permissions, _) = CreateSut([roleKey], attendanceAdminAllowed: false);

        var result = await sut.IsAttendanceAdminAsync(CancellationToken.None);

        Assert.That(result, Is.False);
        permissions.Verify(
            x => x.IsMenuAllowedAsync(It.IsAny<IReadOnlyList<string>>(), AttendanceAdminMenuId, It.IsAny<CancellationToken>()),
            Times.Once);
        foreach (var otherMenuId in new[] { AttendancePageMenuId, AttendanceReportsMenuId, AttendanceAnalyticsMenuId })
        {
            permissions.Verify(
                x => x.IsMenuAllowedAsync(It.IsAny<IReadOnlyList<string>>(), otherMenuId, It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }

    [Test]
    public async Task IsAttendanceAdminAsync_ReturnsFalse_WithNoRoleKeys()
    {
        var (sut, _, _) = CreateSut([], attendanceAdminAllowed: true);

        Assert.That(await sut.IsAttendanceAdminAsync(CancellationToken.None), Is.False);
    }

    [Test]
    public async Task IsAttendanceAdminAsync_WhenAttendanceAdminMenuIsMissing_ReturnsFalseForNonAdmin()
    {
        var (sut, _, _) = CreateSut(["ATTENDANCEOFFICER"], attendanceAdminAllowed: true, includeAttendanceAdminMenu: false);

        Assert.That(await sut.IsAttendanceAdminAsync(CancellationToken.None), Is.False);
    }

    [Test]
    public async Task IsAttendanceAdminAsync_IsCached_SecondCallDoesNotAskAgain()
    {
        var (sut, permissions, menus) = CreateSut(["ATTENDANCEOFFICER"], attendanceAdminAllowed: true);

        var first = await sut.IsAttendanceAdminAsync(CancellationToken.None);
        var second = await sut.IsAttendanceAdminAsync(CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.True);
            Assert.That(second, Is.True);
        });
        menus.Verify(x => x.GetQueryable(), Times.Once);
        permissions.Verify(
            x => x.IsMenuAllowedAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    internal static (AttendanceAdminAccessService Sut, Mock<IPermissionEvaluator> Permissions, Mock<IBaseRepository<Menu>> Menus) CreateSut(
        string[] roleKeys,
        bool attendanceAdminAllowed = false,
        bool includeAttendanceAdminMenu = true)
    {
        var identity = new Mock<IIdentityContext>();
        identity.Setup(x => x.GetCurrent()).Returns(new UserIdentitySnapshot { RoleKeys = roleKeys.ToList() });

        var permissions = new Mock<IPermissionEvaluator>();
        permissions
            .Setup(x => x.IsMenuAllowedAsync(It.IsAny<IReadOnlyList<string>>(), AttendanceAdminMenuId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(attendanceAdminAllowed);
        foreach (var otherMenuId in new[] { AttendancePageMenuId, AttendanceReportsMenuId, AttendanceAnalyticsMenuId })
        {
            permissions
                .Setup(x => x.IsMenuAllowedAsync(It.IsAny<IReadOnlyList<string>>(), otherMenuId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
        }

        var menuRows = new List<Menu>
        {
            new() { Id = AttendancePageMenuId, Key = "attendance" },
            new() { Id = AttendanceReportsMenuId, Key = "attendance.reports" },
            new() { Id = AttendanceAnalyticsMenuId, Key = "attendance.analytics" },
        };
        if (includeAttendanceAdminMenu)
            menuRows.Add(new Menu { Id = AttendanceAdminMenuId, Key = "attendance.admin" });

        var menus = new Mock<IBaseRepository<Menu>>();
        menus.Setup(x => x.GetQueryable()).Returns(() => menuRows.BuildMock());

        return (new AttendanceAdminAccessService(identity.Object, permissions.Object, menus.Object), permissions, menus);
    }
}
