using EMS.Application.Services.EmployeePortal;
using EMS.Application.Services.Leave;
using EMS.Application.Services.Manager;
using EMS.Domain.DbModels;
using Moq;
using Pukar.Shared;

namespace EMS.Application.UnitTests.Leave;

/// <summary>
/// B06 U7–U14: who may view, manage and use attachments of another employee's leave.
/// Self: all three. Leave admin: all three. Current direct manager: view only. Everyone else: none.
/// </summary>
public class LeaveEmployeeAccessServiceTests
{
    private const int SelfId = 10;
    private const int TargetId = 20;

    public enum Check
    {
        View,
        Manage,
        Attachments,
    }

    [TestCase(Check.View)]
    [TestCase(Check.Manage)]
    [TestCase(Check.Attachments)]
    public void Self_IsAllowed_EvenWithNoMenus(Check check)
    {
        var sut = CreateSut(linkedId: SelfId);

        Assert.DoesNotThrowAsync(() => Run(sut, check, SelfId));
    }

    [TestCase(Check.View)]
    [TestCase(Check.Manage)]
    [TestCase(Check.Attachments)]
    public void PlainEmployeeWithLeaveMenu_IsDenied_ForAnotherEmployee(Check check)
    {
        // The "leave" menu is not part of the rule at all: not a leave admin, not a manager.
        var sut = CreateSut(linkedId: SelfId);

        AssertDenied(() => Run(sut, check, TargetId));
    }

    [TestCase(Check.View)]
    [TestCase(Check.Manage)]
    [TestCase(Check.Attachments)]
    public void LeaveAdmin_IsAllowed_ForAnotherEmployee(Check check)
    {
        var sut = CreateSut(linkedId: SelfId, isLeaveAdmin: true);

        Assert.DoesNotThrowAsync(() => Run(sut, check, TargetId));
    }

    [Test]
    public void DirectManager_CanView_ButNotManageOrUseAttachments()
    {
        var sut = CreateSut(linkedId: SelfId, canViewTeam: true, isDirectReport: true);

        Assert.DoesNotThrowAsync(() => Run(sut, Check.View, TargetId));
        AssertDenied(() => Run(sut, Check.Manage, TargetId));
        AssertDenied(() => Run(sut, Check.Attachments, TargetId));
    }

    [TestCase(Check.View)]
    [TestCase(Check.Manage)]
    [TestCase(Check.Attachments)]
    public void ManagerWhoIsNotTheCurrentManager_IsDenied(Check check)
    {
        var sut = CreateSut(linkedId: SelfId, canViewTeam: true, isDirectReport: false);

        AssertDenied(() => Run(sut, check, TargetId));
    }

    [Test]
    public void ManagerIdWithoutManagerTeamPermission_IsDeniedToView()
    {
        var sut = CreateSut(linkedId: SelfId, canViewTeam: false, isDirectReport: true);

        AssertDenied(() => Run(sut, Check.View, TargetId));
    }

    [TestCase(Check.View)]
    [TestCase(Check.Manage)]
    [TestCase(Check.Attachments)]
    public void UnlinkedNonAdmin_IsDenied(Check check)
    {
        var sut = CreateSut(linkedId: null, canViewTeam: true, isDirectReport: true);

        AssertDenied(() => Run(sut, check, TargetId));
    }

    [TestCase(new[] { "ADMIN" }, false, true)]
    [TestCase(new[] { "LEAVEOFFICER" }, true, true)]
    [TestCase(new[] { "LEAVEUSER" }, false, false)]
    public async Task CanManageOtherEmployeesLeaveAsync_EqualsLeaveAdminAnswer(
        string[] roleKeys, bool leaveAdminMenuAllowed, bool expected)
    {
        // Uses the real shared leave admin check, so the eligibility flag cannot drift from it.
        var (leaveAdmin, _, _) = LeaveAdminAccessServiceTests.CreateSut(roleKeys, leaveAdminMenuAllowed);
        var sut = new LeaveEmployeeAccessService(
            LinkedService(SelfId).Object,
            leaveAdmin,
            new Mock<IManagerTeamAccessService>().Object);

        var result = await sut.CanManageOtherEmployeesLeaveAsync(CancellationToken.None);

        Assert.That(result, Is.EqualTo(expected));
        Assert.That(result, Is.EqualTo(await leaveAdmin.IsLeaveAdminAsync(CancellationToken.None)));
    }

    private static Task Run(LeaveEmployeeAccessService sut, Check check, int employeeId) => check switch
    {
        Check.View => sut.EnsureCanViewEmployeeLeaveAsync(employeeId, CancellationToken.None),
        Check.Manage => sut.EnsureCanManageEmployeeLeaveAsync(employeeId, CancellationToken.None),
        Check.Attachments => sut.EnsureCanAccessLeaveAttachmentsAsync(employeeId, CancellationToken.None),
        _ => throw new ArgumentOutOfRangeException(nameof(check)),
    };

    private static void AssertDenied(AsyncTestDelegate call)
    {
        var ex = Assert.ThrowsAsync<BusinessRuleException>(call);
        Assert.That(ex!.Message, Is.EqualTo(LeaveAccessMessages.Denied));
    }

    private static Mock<ILinkedEmployeeService> LinkedService(int? linkedId)
    {
        var linked = new Mock<ILinkedEmployeeService>();
        linked.Setup(x => x.TryGetLinkedEmployeeAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(linkedId is null ? null : new Employee { Id = linkedId.Value, OrganizationId = 1 });
        return linked;
    }

    private static LeaveEmployeeAccessService CreateSut(
        int? linkedId,
        bool isLeaveAdmin = false,
        bool canViewTeam = false,
        bool isDirectReport = false)
    {
        var leaveAdmin = new Mock<ILeaveAdminAccessService>();
        leaveAdmin.Setup(x => x.IsLeaveAdminAsync(It.IsAny<CancellationToken>())).ReturnsAsync(isLeaveAdmin);

        var managerTeam = new Mock<IManagerTeamAccessService>();
        managerTeam.Setup(x => x.CanViewTeamDashboardAsync(It.IsAny<CancellationToken>())).ReturnsAsync(canViewTeam);
        managerTeam
            .Setup(x => x.IsDirectReportAsync(It.IsAny<int>(), TargetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(isDirectReport);

        return new LeaveEmployeeAccessService(LinkedService(linkedId).Object, leaveAdmin.Object, managerTeam.Object);
    }
}
