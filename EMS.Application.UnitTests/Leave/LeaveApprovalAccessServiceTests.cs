using EMS.Application.Services.Authorization;
using EMS.Application.Services.EmployeePortal;
using EMS.Application.Services.Leave;
using EMS.Application.Services.Manager;
using EMS.Domain.DbModels;
using EMS.Domain.Enums;
using EMS.Domain.Repositories.Interface;
using MockQueryable;
using Moq;
using Pukar.Shared;

namespace EMS.Application.UnitTests.Leave;

/// <summary>Who may decide a leave request (AC9–AC13). Uses the real ManagerTeamAccessService for the direct-report rule.</summary>
public class LeaveApprovalAccessServiceTests
{
    private const int ManagerTeamMenuId = 42;
    private const int LeaveAdminMenuId = 77;
    private const int SelfServiceLeaveMenuId = 5;
    private const int OrganizationId = 1;

    [Test]
    public async Task EnsureCanDecideAsync_ReturnsReviewerId_ForCurrentDirectManager()
    {
        var sut = CreateSut(
            linked: new Employee { Id = 10, OrganizationId = OrganizationId },
            employees: [new Employee { Id = 20, OrganizationId = OrganizationId, ManagerId = 10 }],
            managerTeamAllowed: true);

        var reviewerId = await sut.EnsureCanDecideAsync(Request(employeeId: 20), CancellationToken.None);

        Assert.That(reviewerId, Is.EqualTo(10));
    }

    [Test]
    public void EnsureCanDecideAsync_Denies_FormerManagerAfterTransfer()
    {
        // Employee 20 now reports to 99; actor 10 was the previous manager.
        var sut = CreateSut(
            linked: new Employee { Id = 10, OrganizationId = OrganizationId },
            employees: [new Employee { Id = 20, OrganizationId = OrganizationId, ManagerId = 99 }],
            managerTeamAllowed: true);

        var ex = Assert.ThrowsAsync<BusinessRuleException>(() =>
            sut.EnsureCanDecideAsync(Request(employeeId: 20), CancellationToken.None));

        Assert.That(ex!.Message, Is.EqualTo(LeaveApprovalMessages.Denied));
    }

    [Test]
    public void EnsureCanDecideAsync_Denies_ManagerWithoutManagerTeamPermission()
    {
        var sut = CreateSut(
            linked: new Employee { Id = 10, OrganizationId = OrganizationId },
            employees: [new Employee { Id = 20, OrganizationId = OrganizationId, ManagerId = 10 }],
            managerTeamAllowed: false);

        var ex = Assert.ThrowsAsync<BusinessRuleException>(() =>
            sut.EnsureCanDecideAsync(Request(employeeId: 20), CancellationToken.None));

        Assert.That(ex!.Message, Is.EqualTo(LeaveApprovalMessages.Denied));
    }

    [Test]
    public void EnsureCanDecideAsync_Denies_ManagerOfArchivedEmployee()
    {
        var sut = CreateSut(
            linked: new Employee { Id = 10, OrganizationId = OrganizationId },
            employees: [new Employee { Id = 20, OrganizationId = OrganizationId, ManagerId = 10, IsArchived = true }],
            managerTeamAllowed: true);

        var ex = Assert.ThrowsAsync<BusinessRuleException>(() =>
            sut.EnsureCanDecideAsync(Request(employeeId: 20), CancellationToken.None));

        Assert.That(ex!.Message, Is.EqualTo(LeaveApprovalMessages.Denied));
    }

    [Test]
    public void EnsureCanDecideAsync_Denies_SelfServiceLeaveMenuOnly()
    {
        // The self-service "leave" menu must not grant HR decisions (only leave.admin does).
        var sut = CreateSut(
            linked: new Employee { Id = 10, OrganizationId = OrganizationId },
            employees: [new Employee { Id = 20, OrganizationId = OrganizationId, ManagerId = 99 }],
            roleKeys: ["EMPLOYEE"],
            leaveAdminAllowed: false);

        var ex = Assert.ThrowsAsync<BusinessRuleException>(() =>
            sut.EnsureCanDecideAsync(Request(employeeId: 20), CancellationToken.None));

        Assert.That(ex!.Message, Is.EqualTo(LeaveApprovalMessages.Denied));
    }

    [Test]
    public async Task EnsureCanDecideAsync_Allows_LeaveAdminMenuForNonReport()
    {
        var sut = CreateSut(
            linked: new Employee { Id = 10, OrganizationId = OrganizationId },
            employees: [new Employee { Id = 20, OrganizationId = OrganizationId, ManagerId = null }],
            roleKeys: ["HR"],
            leaveAdminAllowed: true);

        var reviewerId = await sut.EnsureCanDecideAsync(Request(employeeId: 20), CancellationToken.None);

        Assert.That(reviewerId, Is.EqualTo(10));
    }

    [TestCase("ADMIN")]
    [TestCase("ADMINISTRATOR")]
    public async Task EnsureCanDecideAsync_Allows_AdminRole(string role)
    {
        var sut = CreateSut(
            linked: new Employee { Id = 10, OrganizationId = OrganizationId },
            employees: [new Employee { Id = 20, OrganizationId = OrganizationId, ManagerId = 99 }],
            roleKeys: [role]);

        var reviewerId = await sut.EnsureCanDecideAsync(Request(employeeId: 20), CancellationToken.None);

        Assert.That(reviewerId, Is.EqualTo(10));
    }

    [Test]
    public void EnsureCanDecideAsync_Denies_OwnRequest_EvenForAdmin()
    {
        var sut = CreateSut(
            linked: new Employee { Id = 10, OrganizationId = OrganizationId, ManagerId = 10 },
            employees: [new Employee { Id = 10, OrganizationId = OrganizationId, ManagerId = 10 }],
            roleKeys: ["ADMIN"],
            managerTeamAllowed: true,
            leaveAdminAllowed: true);

        var ex = Assert.ThrowsAsync<BusinessRuleException>(() =>
            sut.EnsureCanDecideAsync(Request(employeeId: 10), CancellationToken.None));

        Assert.That(ex!.Message, Is.EqualTo(LeaveApprovalMessages.SelfDecisionDenied));
    }

    [Test]
    public void EnsureCanDecideAsync_Denies_WhenActorHasNoLinkedEmployee()
    {
        var sut = CreateSut(linked: null, roleKeys: ["ADMIN"]);

        var ex = Assert.ThrowsAsync<BusinessRuleException>(() =>
            sut.EnsureCanDecideAsync(Request(employeeId: 20), CancellationToken.None));

        Assert.That(ex!.Message, Is.EqualTo(LeaveApprovalMessages.ReviewerNotLinked));
    }

    [Test]
    public void EnsureCanDecideAsync_Denies_RequestInAnotherOrganisation()
    {
        var sut = CreateSut(
            linked: new Employee { Id = 10, OrganizationId = 2 },
            roleKeys: ["ADMIN"]);

        var ex = Assert.ThrowsAsync<BusinessRuleException>(() =>
            sut.EnsureCanDecideAsync(Request(employeeId: 20), CancellationToken.None));

        Assert.That(ex!.Message, Is.EqualTo(LeaveApprovalMessages.Denied));
    }

    private static LeaveRequest Request(int employeeId) => new()
    {
        Id = 500,
        OrganizationId = OrganizationId,
        EmployeeId = employeeId,
        LeaveTypeId = 3,
        Status = LeaveRequestStatus.Pending,
    };

    private static LeaveApprovalAccessService CreateSut(
        Employee? linked,
        List<Employee>? employees = null,
        string[]? roleKeys = null,
        bool managerTeamAllowed = false,
        bool leaveAdminAllowed = false)
    {
        var identity = new Mock<IIdentityContext>();
        identity.Setup(x => x.GetCurrent()).Returns(new UserIdentitySnapshot
        {
            RoleKeys = roleKeys?.ToList() ?? ["MANAGER"],
        });

        var linkedService = new Mock<ILinkedEmployeeService>();
        linkedService.Setup(x => x.TryGetLinkedEmployeeAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(linked);

        var permissions = new Mock<IPermissionEvaluator>();
        permissions
            .Setup(x => x.IsMenuAllowedAsync(It.IsAny<IReadOnlyList<string>>(), ManagerTeamMenuId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(managerTeamAllowed);
        permissions
            .Setup(x => x.IsMenuAllowedAsync(It.IsAny<IReadOnlyList<string>>(), LeaveAdminMenuId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(leaveAdminAllowed);
        // Self-service "leave" menu is always allowed here, to prove it does not grant decisions.
        permissions
            .Setup(x => x.IsMenuAllowedAsync(It.IsAny<IReadOnlyList<string>>(), SelfServiceLeaveMenuId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var menus = new Mock<IBaseRepository<Menu>>();
        menus.Setup(x => x.GetQueryable()).Returns(() => new List<Menu>
        {
            new() { Id = ManagerTeamMenuId, Key = "manager.team" },
            new() { Id = LeaveAdminMenuId, Key = "leave.admin" },
            new() { Id = SelfServiceLeaveMenuId, Key = "leave" },
        }.BuildMock());

        var employeeRepo = new Mock<IBaseRepository<Employee>>();
        employeeRepo.Setup(x => x.GetQueryable()).Returns(() => (employees ?? []).BuildMock());

        var managerTeamAccess = new ManagerTeamAccessService(
            identity.Object,
            linkedService.Object,
            permissions.Object,
            menus.Object,
            employeeRepo.Object);

        return new LeaveApprovalAccessService(
            identity.Object,
            linkedService.Object,
            permissions.Object,
            menus.Object,
            managerTeamAccess);
    }
}
