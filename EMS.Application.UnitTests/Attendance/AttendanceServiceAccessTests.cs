using EMS.Application.DTOs.Attendance;
using EMS.Application.Services.Attendance;
using EMS.Application.UnitTests.Infrastructure;
using EMS.Domain.DbModels;
using EMS.Domain.Enums;
using EMS.Domain.Repositories.Interface;
using MockQueryable;
using Moq;
using Pukar.Shared;

namespace EMS.Application.UnitTests.Attendance;

/// <summary>
/// B07: <see cref="AttendanceService"/> uses the attendance access service for every action. Client-supplied times
/// count for attendance admins only, manual entries need an attendance admin, the report scope reaches the
/// repository query and the absenteeism head count, and the existing validation messages are unchanged.
/// </summary>
public class AttendanceServiceAccessTests
{
    private const int OrgId = 1;
    private const int SelfId = 10;
    private const int TeamMateId = 11;
    private const int OtherId = 12;
    private const int DepartmentId = 5;

    private static readonly DateTime ClientTime = new(2020, 1, 1, 8, 0, 0, DateTimeKind.Utc);

    [Test]
    public async Task CheckIn_NonAdminSelfAction_IgnoresClientTime()
    {
        var (sut, records, _) = CreateSut(new AttendanceActionTarget(SelfId, MayUseClientTime: false));

        var result = await sut.CheckInAsync(new CheckInRequestModel { CheckInAtUtc = ClientTime }, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.EmployeeId, Is.EqualTo(SelfId));
            Assert.That(result.CheckInAtUtc, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromMinutes(1)));
            Assert.That(records.Single().CheckInAtUtc, Is.Not.EqualTo(ClientTime));
        });
    }

    [Test]
    public async Task CheckIn_AttendanceAdmin_UsesClientTime_ForTheChosenEmployee()
    {
        var (sut, records, _) = CreateSut(new AttendanceActionTarget(OtherId, MayUseClientTime: true));

        var result = await sut.CheckInAsync(
            new CheckInRequestModel { EmployeeId = OtherId, CheckInAtUtc = ClientTime },
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.EmployeeId, Is.EqualTo(OtherId));
            Assert.That(result.CheckInAtUtc, Is.EqualTo(ClientTime));
            Assert.That(records.Single().WorkDate, Is.EqualTo(ClientTime.Date));
        });
    }

    [Test]
    public void CheckIn_Denied_WritesNothing()
    {
        var (sut, records, access) = CreateSut(target: null);
        access.Setup(x => x.ResolveActionTargetAsync(OtherId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new BusinessRuleException(AttendanceAccessMessages.Denied));

        var ex = Assert.ThrowsAsync<BusinessRuleException>(() =>
            sut.CheckInAsync(new CheckInRequestModel { EmployeeId = OtherId }, CancellationToken.None));

        Assert.Multiple(() =>
        {
            Assert.That(ex!.Message, Is.EqualTo(AttendanceAccessMessages.Denied));
            Assert.That(records, Is.Empty);
        });
    }

    [Test]
    public void CheckOut_AttendanceAdminTimeBeforeCheckIn_KeepsExistingMessage()
    {
        var (sut, records, _) = CreateSut(new AttendanceActionTarget(SelfId, MayUseClientTime: true));
        records.Add(new AttendanceRecord
        {
            Id = 1,
            OrganizationId = OrgId,
            EmployeeId = SelfId,
            WorkDate = ClientTime.Date,
            CheckInAtUtc = ClientTime,
        });

        var ex = Assert.ThrowsAsync<BusinessRuleException>(() => sut.CheckOutAsync(
            new CheckOutRequestModel { CheckOutAtUtc = ClientTime.AddHours(-1) },
            CancellationToken.None));

        Assert.That(ex!.Message, Is.EqualTo("Check-out time cannot be earlier than check-in time."));
    }

    [Test]
    public void ManualEntry_NonAdmin_IsDenied_AndWritesNothing()
    {
        var (sut, records, access) = CreateSut(target: null);
        access.Setup(x => x.EnsureIsAttendanceAdminAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new BusinessRuleException(AttendanceAccessMessages.Denied));

        var ex = Assert.ThrowsAsync<BusinessRuleException>(() => sut.CreateManualEntryAsync(ManualEntry(SelfId, OrgId), CancellationToken.None));

        Assert.Multiple(() =>
        {
            Assert.That(ex!.Message, Is.EqualTo(AttendanceAccessMessages.Denied));
            Assert.That(records, Is.Empty);
        });
        access.Verify(x => x.EnsureIsAttendanceAdminAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void ManualEntry_AttendanceAdmin_KeepsExistingValidationMessages()
    {
        var (sut, _, _) = CreateSut(target: null);

        var wrongOrg = Assert.ThrowsAsync<BusinessRuleException>(() =>
            sut.CreateManualEntryAsync(ManualEntry(SelfId, organizationId: 99), CancellationToken.None));
        var backwards = ManualEntry(SelfId, OrgId);
        backwards.CheckOutAtUtc = backwards.CheckInAtUtc.AddHours(-1);
        var reversed = Assert.ThrowsAsync<BusinessRuleException>(() =>
            sut.CreateManualEntryAsync(backwards, CancellationToken.None));

        Assert.Multiple(() =>
        {
            Assert.That(wrongOrg!.Message, Is.EqualTo("Employee must belong to the same organization."));
            Assert.That(reversed!.Message, Is.EqualTo("Check-out must be later than check-in."));
        });
    }

    [Test]
    public async Task ManualEntry_AttendanceAdmin_CreatesAutoApprovedRecord()
    {
        var (sut, records, _) = CreateSut(target: null);

        var result = await sut.CreateManualEntryAsync(ManualEntry(OtherId, OrgId), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.EmployeeId, Is.EqualTo(OtherId));
            Assert.That(records.Single().Status, Is.EqualTo(AttendanceStatus.AutoApproved));
        });
    }

    [Test]
    public void EmployeeReads_AskTheViewCheck_AndStopWhenDenied()
    {
        var (sut, _, access) = CreateSut(target: null);
        access.Setup(x => x.EnsureCanViewEmployeeAttendanceAsync(OtherId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new BusinessRuleException(AttendanceAccessMessages.Denied));

        Assert.ThrowsAsync<BusinessRuleException>(() => sut.GetEmployeeAttendanceAsync(OtherId, null, null, CancellationToken.None));
        Assert.ThrowsAsync<BusinessRuleException>(() =>
            sut.GetSummaryAsync(OtherId, ClientTime, ClientTime, CancellationToken.None));
        access.Verify(x => x.EnsureCanViewEmployeeAttendanceAsync(OtherId, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Test]
    public async Task Reports_PassTheScopeIntoTheRepositoryQuery()
    {
        var (sut, _, access) = CreateSut(target: null, out var repository);
        var allowed = new[] { SelfId, TeamMateId };
        access.Setup(x => x.ResolveReportScopeAsync(OrgId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(AttendanceReportScope.Only(allowed));
        var filter = Filter();

        await sut.GetDailySummariesAsync(filter, CancellationToken.None);
        await sut.GetPeriodSummariesAsync(filter, CancellationToken.None);
        await sut.GetPunctualityAnalyticsAsync(filter, CancellationToken.None);
        await sut.GetAbsenteeismAnalyticsAsync(filter, CancellationToken.None);
        await sut.ExportReportAsync(
            new AttendanceExportRequestModel { OrganizationId = OrgId, FromDate = filter.FromDate, ToDate = filter.ToDate, Format = "csv" },
            CancellationToken.None);

        repository.Verify(
            x => x.GetRecordsForReportAsync(
                OrgId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), null, null,
                It.Is<IReadOnlyCollection<int>?>(ids => ids != null && ids.OrderBy(i => i).SequenceEqual(allowed)),
                It.IsAny<CancellationToken>()),
            Times.Exactly(5));
        access.Verify(x => x.ResolveReportScopeAsync(OrgId, null, It.IsAny<CancellationToken>()), Times.Exactly(5));
    }

    [Test]
    public async Task Absenteeism_HeadCount_CountsOnlyTheScope_NarrowedByDepartment()
    {
        var (sut, _, access) = CreateSut(target: null);
        access.Setup(x => x.ResolveReportScopeAsync(OrgId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(AttendanceReportScope.Only([SelfId, TeamMateId]));

        // One weekday: expected work days equal the head count.
        var whole = await sut.GetAbsenteeismAnalyticsAsync(Filter(), CancellationToken.None);
        var filter = Filter();
        filter.DepartmentId = DepartmentId;
        var department = await sut.GetAbsenteeismAnalyticsAsync(filter, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(whole.ExpectedWorkDays, Is.EqualTo(2), "Scope {self, team mate}, not the organisation's 3.");
            Assert.That(department.ExpectedWorkDays, Is.EqualTo(1), "Only the team mate is in the department.");
        });
    }

    [Test]
    public async Task Absenteeism_HeadCount_Unrestricted_IsTheOrganisationCount()
    {
        var (sut, _, access) = CreateSut(target: null);
        access.Setup(x => x.ResolveReportScopeAsync(OrgId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(AttendanceReportScope.Unrestricted);

        var result = await sut.GetAbsenteeismAnalyticsAsync(Filter(), CancellationToken.None);

        Assert.That(result.ExpectedWorkDays, Is.EqualTo(3));
    }

    private static AttendanceReportFilterRequestModel Filter() => new()
    {
        OrganizationId = OrgId,
        // Monday.
        FromDate = new DateTime(2027, 2, 1),
        ToDate = new DateTime(2027, 2, 1),
    };

    private static ManualAttendanceEntryRequestModel ManualEntry(int employeeId, int organizationId) => new()
    {
        OrganizationId = organizationId,
        EmployeeId = employeeId,
        WorkDate = new DateTime(2027, 3, 1),
        CheckInAtUtc = new DateTime(2027, 3, 1, 8, 0, 0, DateTimeKind.Utc),
        CheckOutAtUtc = new DateTime(2027, 3, 1, 16, 0, 0, DateTimeKind.Utc),
        ManualReason = "Unit test",
    };

    private static (AttendanceService Sut, List<AttendanceRecord> Records, Mock<IAttendanceEmployeeAccessService> Access) CreateSut(
        AttendanceActionTarget? target)
        => CreateSut(target, out _);

    private static (AttendanceService Sut, List<AttendanceRecord> Records, Mock<IAttendanceEmployeeAccessService> Access) CreateSut(
        AttendanceActionTarget? target,
        out Mock<IAttendanceRepository> attendanceRepository)
    {
        var records = new List<AttendanceRecord>();

        var attendance = new Mock<IAttendanceRepository>();
        attendance.Setup(x => x.GetOpenForEmployeeAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int employeeId, CancellationToken _) =>
                records.LastOrDefault(r => r.EmployeeId == employeeId && r.CheckOutAtUtc == null));
        attendance.Setup(x => x.AddAsync(It.IsAny<AttendanceRecord>(), It.IsAny<CancellationToken>()))
            .Callback<AttendanceRecord, CancellationToken>((r, _) => records.Add(r))
            .Returns(Task.CompletedTask);
        attendance.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        attendance.Setup(x => x.GetQueryable()).Returns(() => records.BuildMock());
        attendance
            .Setup(x => x.GetRecordsForReportAsync(
                It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int?>(), It.IsAny<int?>(),
                It.IsAny<IReadOnlyCollection<int>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<AttendanceRecord>)Array.Empty<AttendanceRecord>());

        var breaks = new Mock<IAttendanceBreakRepository>();
        breaks.Setup(x => x.GetOpenForAttendanceAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AttendanceBreak?)null);
        breaks.Setup(x => x.GetQueryable())
            .Returns(() => new List<AttendanceBreak>().BuildMock());

        var employees = new InMemoryRepositoryMock<Employee>(e => e.Id, (e, id) => e.Id = id);
        employees.Seed(
            new Employee { Id = SelfId, OrganizationId = OrgId },
            new Employee { Id = TeamMateId, OrganizationId = OrgId, ManagerId = SelfId, DepartmentId = DepartmentId },
            new Employee { Id = OtherId, OrganizationId = OrgId });

        var policies = new InMemoryRepositoryMock<AttendancePolicy>(p => p.Id, (p, id) => p.Id = id);

        var access = new Mock<IAttendanceEmployeeAccessService>();
        if (target is not null)
        {
            access.Setup(x => x.ResolveActionTargetAsync(It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(target);
        }

        var sut = new AttendanceService(
            attendance.Object,
            breaks.Object,
            employees.CreateMock().Object,
            policies.CreateMock().Object,
            access.Object);
        attendanceRepository = attendance;
        return (sut, records, access);
    }
}
