using EMS.Application.Services.Attendance;
using EMS.Application.Services.EmployeePortal;
using EMS.Application.Services.Manager;
using EMS.Application.UnitTests.Infrastructure;
using EMS.Domain.DbModels;
using Moq;
using Pukar.Shared;

namespace EMS.Application.UnitTests.Attendance;

/// <summary>
/// B07: whose attendance the current user may record (self action target), read (per-employee view) and report on
/// (report scope).
/// </summary>
public class AttendanceEmployeeAccessServiceTests
{
    private const int OrgId = 1;
    private const int OtherOrgId = 2;
    private const int SelfId = 10;
    private const int TargetId = 20;
    private const int ReportId = 30;
    private const int ArchivedReportId = 31;
    private const int OutsiderId = 40;

    // ----- Self action target -----

    [Test]
    public async Task ResolveActionTarget_IdOmitted_IsLinkedEmployee_WithServerTime()
    {
        var sut = CreateSut(linkedId: SelfId);

        var nullId = await sut.ResolveActionTargetAsync(null, CancellationToken.None);
        var zeroId = await sut.ResolveActionTargetAsync(0, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(nullId, Is.EqualTo(new AttendanceActionTarget(SelfId, MayUseClientTime: false)));
            Assert.That(zeroId, Is.EqualTo(new AttendanceActionTarget(SelfId, MayUseClientTime: false)));
        });
    }

    [Test]
    public async Task ResolveActionTarget_OwnId_IsLinkedEmployee()
    {
        var sut = CreateSut(linkedId: SelfId);

        var target = await sut.ResolveActionTargetAsync(SelfId, CancellationToken.None);

        Assert.That(target, Is.EqualTo(new AttendanceActionTarget(SelfId, MayUseClientTime: false)));
    }

    [Test]
    public void ResolveActionTarget_OtherId_NotAttendanceAdmin_IsDenied_EvenForDirectManager()
    {
        var sut = CreateSut(linkedId: SelfId, canViewTeam: true, isDirectReport: true);

        AssertDenied(() => sut.ResolveActionTargetAsync(TargetId, CancellationToken.None));
    }

    [Test]
    public async Task ResolveActionTarget_OtherId_AttendanceAdmin_IsThatEmployee_WithClientTime()
    {
        var sut = CreateSut(linkedId: SelfId, isAttendanceAdmin: true);

        var target = await sut.ResolveActionTargetAsync(TargetId, CancellationToken.None);

        Assert.That(target, Is.EqualTo(new AttendanceActionTarget(TargetId, MayUseClientTime: true)));
    }

    [Test]
    public void ResolveActionTarget_UnlinkedNonAdmin_IsDenied()
    {
        var sut = CreateSut(linkedId: null);

        AssertDenied(() => sut.ResolveActionTargetAsync(null, CancellationToken.None));
        AssertDenied(() => sut.ResolveActionTargetAsync(TargetId, CancellationToken.None));
    }

    [Test]
    public void ResolveActionTarget_UnlinkedAttendanceAdmin_NoId_MustChooseAnEmployee()
    {
        var sut = CreateSut(linkedId: null, isAttendanceAdmin: true);

        var ex = Assert.ThrowsAsync<BusinessRuleException>(() => sut.ResolveActionTargetAsync(null, CancellationToken.None));
        Assert.That(ex!.Message, Is.EqualTo(AttendanceAccessMessages.ChooseEmployee));
    }

    [Test]
    public async Task ResolveActionTarget_UnlinkedAttendanceAdmin_WithId_IsThatEmployee()
    {
        var sut = CreateSut(linkedId: null, isAttendanceAdmin: true);

        var target = await sut.ResolveActionTargetAsync(TargetId, CancellationToken.None);

        Assert.That(target, Is.EqualTo(new AttendanceActionTarget(TargetId, MayUseClientTime: true)));
    }

    // ----- Manual entry -----

    [Test]
    public void EnsureIsAttendanceAdmin_DeniesSelfAndManagers_AllowsAttendanceAdmin()
    {
        AssertDenied(() => CreateSut(linkedId: SelfId, canViewTeam: true, isDirectReport: true)
            .EnsureIsAttendanceAdminAsync(CancellationToken.None));
        Assert.DoesNotThrowAsync(() => CreateSut(linkedId: null, isAttendanceAdmin: true)
            .EnsureIsAttendanceAdminAsync(CancellationToken.None));
    }

    // ----- Per-employee view -----

    [Test]
    public void View_Self_IsAllowed_WithNoMenus()
    {
        var sut = CreateSut(linkedId: SelfId);

        Assert.DoesNotThrowAsync(() => sut.EnsureCanViewEmployeeAttendanceAsync(SelfId, CancellationToken.None));
    }

    [Test]
    public void View_AttendanceAdmin_IsAllowed()
    {
        var sut = CreateSut(linkedId: null, isAttendanceAdmin: true);

        Assert.DoesNotThrowAsync(() => sut.EnsureCanViewEmployeeAttendanceAsync(TargetId, CancellationToken.None));
    }

    [Test]
    public void View_DirectManagerWithManagerTeam_IsAllowed()
    {
        var sut = CreateSut(linkedId: SelfId, canViewTeam: true, isDirectReport: true);

        Assert.DoesNotThrowAsync(() => sut.EnsureCanViewEmployeeAttendanceAsync(TargetId, CancellationToken.None));
    }

    [Test]
    public void View_ManagerWithoutManagerTeam_IsDenied()
    {
        var sut = CreateSut(linkedId: SelfId, canViewTeam: false, isDirectReport: true);

        AssertDenied(() => sut.EnsureCanViewEmployeeAttendanceAsync(TargetId, CancellationToken.None));
    }

    [Test]
    public void View_NotADirectReport_IsDenied()
    {
        var sut = CreateSut(linkedId: SelfId, canViewTeam: true, isDirectReport: false);

        AssertDenied(() => sut.EnsureCanViewEmployeeAttendanceAsync(TargetId, CancellationToken.None));
    }

    [Test]
    public void View_Unlinked_IsDenied()
    {
        var sut = CreateSut(linkedId: null, canViewTeam: true, isDirectReport: true);

        AssertDenied(() => sut.EnsureCanViewEmployeeAttendanceAsync(TargetId, CancellationToken.None));
    }

    // ----- Report scope -----

    [Test]
    public async Task ReportScope_AttendanceAdmin_IsUnrestricted_AnyOrganisationAndEmployee()
    {
        var sut = CreateSut(linkedId: SelfId, isAttendanceAdmin: true);

        var scope = await sut.ResolveReportScopeAsync(OtherOrgId, OutsiderId, CancellationToken.None);

        Assert.That(scope.IsUnrestricted, Is.True);
    }

    [Test]
    public async Task ReportScope_Manager_IsSelfPlusCurrentNonArchivedReports()
    {
        var sut = CreateSut(linkedId: SelfId, canViewTeam: true);

        var scope = await sut.ResolveReportScopeAsync(OrgId, null, CancellationToken.None);

        Assert.That(scope.EmployeeIds, Is.EquivalentTo(new[] { SelfId, ReportId }));
    }

    [Test]
    public async Task ReportScope_PlainEmployee_IsSelfOnly()
    {
        var sut = CreateSut(linkedId: SelfId);

        var scope = await sut.ResolveReportScopeAsync(OrgId, null, CancellationToken.None);

        Assert.That(scope.EmployeeIds, Is.EquivalentTo(new[] { SelfId }));
    }

    [Test]
    public void ReportScope_RequestedEmployeeOutsideTheSet_IsDenied()
    {
        AssertDenied(() => CreateSut(linkedId: SelfId).ResolveReportScopeAsync(OrgId, ReportId, CancellationToken.None));
        AssertDenied(() => CreateSut(linkedId: SelfId, canViewTeam: true)
            .ResolveReportScopeAsync(OrgId, OutsiderId, CancellationToken.None));
        AssertDenied(() => CreateSut(linkedId: SelfId, canViewTeam: true)
            .ResolveReportScopeAsync(OrgId, ArchivedReportId, CancellationToken.None));
    }

    [Test]
    public void ReportScope_AnotherOrganisation_ForNonAdmin_IsDenied()
    {
        var sut = CreateSut(linkedId: SelfId, canViewTeam: true);

        AssertDenied(() => sut.ResolveReportScopeAsync(OtherOrgId, null, CancellationToken.None));
    }

    [Test]
    public void ReportScope_Unlinked_IsDenied()
    {
        var sut = CreateSut(linkedId: null, canViewTeam: true);

        AssertDenied(() => sut.ResolveReportScopeAsync(OrgId, null, CancellationToken.None));
    }

    [Test]
    public async Task ReportScope_RequestedEmployeeInsideTheSet_IsThatEmployeeOnly()
    {
        var sut = CreateSut(linkedId: SelfId, canViewTeam: true);

        var scope = await sut.ResolveReportScopeAsync(OrgId, ReportId, CancellationToken.None);

        Assert.That(scope.EmployeeIds, Is.EquivalentTo(new[] { ReportId }));
    }

    // ----- Portal flag -----

    [TestCase(new[] { "ADMIN" }, false, true)]
    [TestCase(new[] { "ATTENDANCEOFFICER" }, true, true)]
    [TestCase(new[] { "ATTENDANCEUSER" }, false, false)]
    public async Task CanManageOtherEmployeesAttendance_EqualsAttendanceAdminAnswer(
        string[] roleKeys, bool attendanceAdminMenuAllowed, bool expected)
    {
        // Uses the real shared attendance admin check, so the eligibility flag cannot drift from it.
        var (attendanceAdmin, _, _) = AttendanceAdminAccessServiceTests.CreateSut(roleKeys, attendanceAdminMenuAllowed);
        var sut = new AttendanceEmployeeAccessService(
            LinkedService(SelfId).Object,
            attendanceAdmin,
            new Mock<IManagerTeamAccessService>().Object,
            EmployeeRepository().CreateMock().Object);

        var result = await sut.CanManageOtherEmployeesAttendanceAsync(CancellationToken.None);

        Assert.That(result, Is.EqualTo(expected));
        Assert.That(result, Is.EqualTo(await attendanceAdmin.IsAttendanceAdminAsync(CancellationToken.None)));
    }

    private static void AssertDenied(AsyncTestDelegate call)
    {
        var ex = Assert.ThrowsAsync<BusinessRuleException>(call);
        Assert.That(ex!.Message, Is.EqualTo(AttendanceAccessMessages.Denied));
    }

    private static Mock<ILinkedEmployeeService> LinkedService(int? linkedId)
    {
        var linked = new Mock<ILinkedEmployeeService>();
        linked.Setup(x => x.TryGetLinkedEmployeeAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(linkedId is null ? null : new Employee { Id = linkedId.Value, OrganizationId = OrgId });
        return linked;
    }

    private static InMemoryRepositoryMock<Employee> EmployeeRepository()
    {
        var employees = new InMemoryRepositoryMock<Employee>(e => e.Id, (e, id) => e.Id = id);
        employees.Seed(
            new Employee { Id = SelfId, OrganizationId = OrgId },
            new Employee { Id = TargetId, OrganizationId = OrgId },
            new Employee { Id = ReportId, OrganizationId = OrgId, ManagerId = SelfId },
            new Employee { Id = ArchivedReportId, OrganizationId = OrgId, ManagerId = SelfId, IsArchived = true },
            new Employee { Id = OutsiderId, OrganizationId = OrgId, ManagerId = TargetId });
        return employees;
    }

    private static AttendanceEmployeeAccessService CreateSut(
        int? linkedId,
        bool isAttendanceAdmin = false,
        bool canViewTeam = false,
        bool isDirectReport = false)
    {
        var attendanceAdmin = new Mock<IAttendanceAdminAccessService>();
        attendanceAdmin.Setup(x => x.IsAttendanceAdminAsync(It.IsAny<CancellationToken>())).ReturnsAsync(isAttendanceAdmin);

        var managerTeam = new Mock<IManagerTeamAccessService>();
        managerTeam.Setup(x => x.CanViewTeamDashboardAsync(It.IsAny<CancellationToken>())).ReturnsAsync(canViewTeam);
        managerTeam
            .Setup(x => x.IsDirectReportAsync(It.IsAny<int>(), TargetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(isDirectReport);

        return new AttendanceEmployeeAccessService(
            LinkedService(linkedId).Object,
            attendanceAdmin.Object,
            managerTeam.Object,
            EmployeeRepository().CreateMock().Object);
    }
}
