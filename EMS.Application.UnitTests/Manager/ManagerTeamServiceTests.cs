using EMS.Application.DTOs.Employee;
using EMS.Application.DTOs.Manager;
using EMS.Application.Services.Authorization;
using EMS.Application.Services.Employees;
using EMS.Application.Services.Manager;
using EMS.Application.UnitTests.Infrastructure;
using EMS.Domain.DbModels;
using EMS.Domain.Enums;
using EMS.Domain.Repositories.Interface;
using Moq;
using Pukar.Shared;

namespace EMS.Application.UnitTests.Manager;

public class ManagerTeamServiceTests
{
    [Test]
    public async Task GetDashboardAsync_ReturnsEmptySummary_WhenNoDirectReports()
    {
        var manager = new Employee
        {
            Id = 5,
            OrganizationId = 1,
            EmployeeNumber = "EMP005",
            FirstName = "Team",
            LastName = "Lead",
            IsArchived = false,
        };

        var employeeRepo = new InMemoryRepositoryMock<Employee>(e => e.Id, (e, id) => e.Id = id);
        employeeRepo.Seed(manager);
        var employees = employeeRepo.CreateMock();

        var access = new Mock<IManagerTeamAccessService>();
        access.Setup(x => x.ResolveEffectiveManagerIdAsync(1, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        var directory = new Mock<IEmployeeDirectoryService>();
        directory.Setup(x => x.QueryAsync(It.IsAny<EmployeeDirectoryQueryModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedEmployeeDirectoryResponseModel
            {
                Items = [],
                TotalCount = 0,
                Page = 1,
                PageSize = 25,
            });

        var sut = CreateService(access.Object, directory.Object, employees.Object);

        var result = await sut.GetDashboardAsync(new ManagerTeamQueryModel { OrganizationId = 1 }, CancellationToken.None);

        Assert.That(result.Summary.ActiveEmployeeCount, Is.EqualTo(0));
        Assert.That(result.Summary.PendingLeaveRequestCount, Is.EqualTo(0));
        Assert.That(result.TotalCount, Is.EqualTo(0));
        Assert.That(result.ManagerId, Is.EqualTo(5));
    }

    [Test]
    public async Task GetDashboardAsync_PassesManagerIdFilter_ToDirectory()
    {
        var manager = new Employee
        {
            Id = 5,
            OrganizationId = 1,
            EmployeeNumber = "EMP005",
            FirstName = "Team",
            LastName = "Lead",
            IsArchived = false,
        };
        var report = new Employee
        {
            Id = 10,
            OrganizationId = 1,
            ManagerId = 5,
            EmployeeNumber = "EMP010",
            FirstName = "Direct",
            LastName = "Report",
            EmploymentStatus = EmploymentStatus.Active,
            IsActive = true,
            IsArchived = false,
            Email = "report@example.com",
        };
        var other = new Employee
        {
            Id = 20,
            OrganizationId = 1,
            ManagerId = 99,
            EmployeeNumber = "EMP020",
            FirstName = "Other",
            LastName = "Team",
            EmploymentStatus = EmploymentStatus.Active,
            IsActive = true,
            IsArchived = false,
            Email = "other@example.com",
        };

        var employeeRepo = new InMemoryRepositoryMock<Employee>(e => e.Id, (e, id) => e.Id = id);
        employeeRepo.Seed(manager, report, other);
        var employees = employeeRepo.CreateMock();

        var access = new Mock<IManagerTeamAccessService>();
        access.Setup(x => x.ResolveEffectiveManagerIdAsync(1, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        EmployeeDirectoryQueryModel? captured = null;
        var directory = new Mock<IEmployeeDirectoryService>();
        directory.Setup(x => x.QueryAsync(It.IsAny<EmployeeDirectoryQueryModel>(), It.IsAny<CancellationToken>()))
            .Callback<EmployeeDirectoryQueryModel, CancellationToken>((q, _) => captured = q)
            .ReturnsAsync(new PagedEmployeeDirectoryResponseModel
            {
                Items =
                [
                    new EmployeeDirectoryItemResponseModel
                    {
                        Id = 10,
                        EmployeeNumber = "EMP010",
                        FirstName = "Direct",
                        LastName = "Report",
                        Email = "report@example.com",
                        EmploymentStatus = EmploymentStatus.Active,
                        IsActive = true,
                    },
                ],
                TotalCount = 1,
                Page = 1,
                PageSize = 25,
            });

        var leaveRepo = new InMemoryRepositoryMock<LeaveRequest>(r => r.Id, (r, id) => r.Id = id);
        leaveRepo.Seed(new LeaveRequest
        {
            Id = 1,
            OrganizationId = 1,
            EmployeeId = 10,
            Status = LeaveRequestStatus.Pending,
            StartDateUtc = DateTime.UtcNow.Date,
            EndDateUtc = DateTime.UtcNow.Date,
        });
        var leaves = leaveRepo.CreateMock();

        var sut = CreateService(
            access.Object,
            directory.Object,
            employees.Object,
            leaves.Object);

        var result = await sut.GetDashboardAsync(
            new ManagerTeamQueryModel { OrganizationId = 1, ManagerId = 5 },
            CancellationToken.None);

        Assert.That(captured, Is.Not.Null);
        Assert.That(captured!.ManagerId, Is.EqualTo(5));
        Assert.That(result.Summary.ActiveEmployeeCount, Is.EqualTo(1));
        Assert.That(result.Summary.PendingLeaveRequestCount, Is.EqualTo(1));
        Assert.That(result.Items, Has.Count.EqualTo(1));
        Assert.That(result.Items[0].PendingLeaveCount, Is.EqualTo(1));
    }

    [Test]
    public async Task GetDashboardAsync_ScopesSummary_ToDirectReportsOnly()
    {
        var manager = new Employee { Id = 5, OrganizationId = 1, EmployeeNumber = "EMP005", FirstName = "M", LastName = "L", IsArchived = false };
        var report = new Employee
        {
            Id = 10,
            OrganizationId = 1,
            ManagerId = 5,
            EmploymentStatus = EmploymentStatus.Active,
            IsActive = true,
            IsArchived = false,
            EmployeeNumber = "EMP010",
            FirstName = "A",
            LastName = "B",
            Email = "a@x.com",
        };
        var outsider = new Employee
        {
            Id = 20,
            OrganizationId = 1,
            ManagerId = 99,
            EmploymentStatus = EmploymentStatus.Active,
            IsActive = true,
            IsArchived = false,
            EmployeeNumber = "EMP020",
            FirstName = "C",
            LastName = "D",
            Email = "c@x.com",
        };

        var employeeRepo = new InMemoryRepositoryMock<Employee>(e => e.Id, (e, id) => e.Id = id);
        employeeRepo.Seed(manager, report, outsider);
        var employees = employeeRepo.CreateMock();

        var access = new Mock<IManagerTeamAccessService>();
        access.Setup(x => x.ResolveEffectiveManagerIdAsync(1, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        var directory = new Mock<IEmployeeDirectoryService>();
        directory.Setup(x => x.QueryAsync(It.IsAny<EmployeeDirectoryQueryModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedEmployeeDirectoryResponseModel { Items = [], TotalCount = 0, Page = 1, PageSize = 25 });

        var leaveRepo = new InMemoryRepositoryMock<LeaveRequest>(r => r.Id, (r, id) => r.Id = id);
        leaveRepo.Seed(
            new LeaveRequest { Id = 1, OrganizationId = 1, EmployeeId = 10, Status = LeaveRequestStatus.Pending, StartDateUtc = DateTime.UtcNow, EndDateUtc = DateTime.UtcNow },
            new LeaveRequest { Id = 2, OrganizationId = 1, EmployeeId = 20, Status = LeaveRequestStatus.Pending, StartDateUtc = DateTime.UtcNow, EndDateUtc = DateTime.UtcNow });
        var leaves = leaveRepo.CreateMock();

        var taskRepo = new InMemoryRepositoryMock<TaskItem>(t => t.Id, (t, id) => t.Id = id);
        taskRepo.Seed(
            new TaskItem { Id = 1, EmployeeId = 10, Status = TaskWorkflowStatus.Assigned, DueAtUtc = DateTime.UtcNow.AddDays(-1), AssignedAtUtc = DateTime.UtcNow, CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow },
            new TaskItem { Id = 2, EmployeeId = 20, Status = TaskWorkflowStatus.Assigned, DueAtUtc = DateTime.UtcNow.AddDays(-1), AssignedAtUtc = DateTime.UtcNow, CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow });
        var tasks = taskRepo.CreateMock();

        var sut = CreateService(access.Object, directory.Object, employees.Object, leaves.Object, tasks.Object);

        var result = await sut.GetDashboardAsync(new ManagerTeamQueryModel { OrganizationId = 1 }, CancellationToken.None);

        Assert.That(result.Summary.PendingLeaveRequestCount, Is.EqualTo(1));
        Assert.That(result.Summary.OverdueTaskCount, Is.EqualTo(1));
        Assert.That(result.Summary.ActiveEmployeeCount, Is.EqualTo(1));
    }

    private static ManagerTeamService CreateService(
        IManagerTeamAccessService access,
        IEmployeeDirectoryService directory,
        IBaseRepository<Employee> employees,
        IBaseRepository<LeaveRequest>? leaveRequests = null,
        IBaseRepository<TaskItem>? tasks = null,
        IBaseRepository<AttendanceRecord>? attendance = null,
        IBaseRepository<EmployeeScheduledChange>? scheduledChanges = null)
    {
        var identity = new Mock<IIdentityContext>();
        identity.Setup(x => x.GetCurrent()).Returns(new UserIdentitySnapshot { RoleKeys = ["MANAGER"] });

        leaveRequests ??= new InMemoryRepositoryMock<LeaveRequest>(r => r.Id, (r, id) => r.Id = id).CreateMock().Object;
        tasks ??= new InMemoryRepositoryMock<TaskItem>(t => t.Id, (t, id) => t.Id = id).CreateMock().Object;
        attendance ??= new InMemoryRepositoryMock<AttendanceRecord>(a => a.Id, (a, id) => a.Id = id).CreateMock().Object;
        scheduledChanges ??= new InMemoryRepositoryMock<EmployeeScheduledChange>(c => c.Id, (c, id) => c.Id = id).CreateMock().Object;

        return new ManagerTeamService(
            access,
            directory,
            identity.Object,
            employees,
            leaveRequests,
            attendance,
            tasks,
            scheduledChanges);
    }
}
