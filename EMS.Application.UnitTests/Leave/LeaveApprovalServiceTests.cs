using EMS.Application.DTOs.Leave;
using EMS.Application.DTOs.Manager;
using EMS.Application.Services.Leave;
using EMS.Application.Services.Manager;
using EMS.Application.Services.Notifications;
using EMS.Domain.DbModels;
using EMS.Domain.Enums;
using EMS.Domain.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using MockQueryable;
using Moq;
using Pukar.Shared;

namespace EMS.Application.UnitTests.Leave;

/// <summary>Approve/reject rules, balance consumption, concurrency and the manager queue (AC1–AC8, AC14, AC15, AC17).</summary>
public class LeaveApprovalServiceTests
{
    private const int ReviewerId = 10;

    private Mock<ILeaveRequestRepository> _requests = null!;
    private Mock<ILeaveBalanceRepository> _balances = null!;
    private Mock<ILeaveTypeRepository> _leaveTypes = null!;
    private Mock<IBaseRepository<Employee>> _employees = null!;
    private Mock<ILeaveApprovalAccessService> _access = null!;
    private Mock<IManagerTeamAccessService> _managerTeamAccess = null!;
    private Mock<IEmsNotificationProducer> _notifications = null!;

    private LeaveRequest _request = null!;
    private LeaveBalance? _balance;

    [SetUp]
    public void SetUp()
    {
        _request = new LeaveRequest
        {
            Id = 500,
            OrganizationId = 1,
            EmployeeId = 20,
            LeaveTypeId = 3,
            RequestedAmount = 2,
            Status = LeaveRequestStatus.Pending,
        };
        _balance = new LeaveBalance
        {
            EmployeeId = 20,
            LeaveTypeId = 3,
            OpeningBalance = 5,
            AccruedAmount = 0,
            AdjustedAmount = 0,
            CarryForwardAmount = 0,
            UsedAmount = 1,
        };

        _requests = new Mock<ILeaveRequestRepository>();
        _requests.Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken _) => id == _request.Id ? _request : null);
        _requests.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        _balances = new Mock<ILeaveBalanceRepository>();
        _balances.Setup(x => x.GetByEmployeeAndTypeAsync(20, 3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => _balance);

        _leaveTypes = new Mock<ILeaveTypeRepository>();
        _employees = new Mock<IBaseRepository<Employee>>();

        _access = new Mock<ILeaveApprovalAccessService>();
        _access.Setup(x => x.EnsureCanDecideAsync(It.IsAny<LeaveRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ReviewerId);

        _managerTeamAccess = new Mock<IManagerTeamAccessService>();

        _notifications = new Mock<IEmsNotificationProducer>();
        _notifications.Setup(x => x.NotifyLeaveApprovedAsync(It.IsAny<LeaveRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _notifications.Setup(x => x.NotifyLeaveRejectedAsync(It.IsAny<LeaveRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private LeaveApprovalService CreateSut() => new(
        _requests.Object,
        _balances.Object,
        _leaveTypes.Object,
        _employees.Object,
        _access.Object,
        _managerTeamAccess.Object,
        _notifications.Object);

    // ---------- Approve ----------

    [TestCase(LeaveRequestStatus.Pending)]
    [TestCase(LeaveRequestStatus.ModifiedPending)]
    public async Task ApproveAsync_ApprovesAndConsumesBalance(LeaveRequestStatus status)
    {
        _request.Status = status;
        var before = DateTime.UtcNow;

        var result = await CreateSut().ApproveAsync(
            500,
            new ApproveLeaveRequestRequestModel { Comment = "  Enjoy the break  " },
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(LeaveRequestStatus.Approved));
            Assert.That(result.ReviewedByEmployeeId, Is.EqualTo(ReviewerId));
            Assert.That(result.ReviewedAtUtc, Is.GreaterThanOrEqualTo(before));
            Assert.That(result.DecisionComment, Is.EqualTo("Enjoy the break"));
            Assert.That(_request.UpdatedAtUtc, Is.GreaterThanOrEqualTo(before));
            Assert.That(_balance!.UsedAmount, Is.EqualTo(3m));
        });
        _balances.Verify(x => x.Update(_balance!), Times.Once);
        _requests.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _notifications.Verify(x => x.NotifyLeaveApprovedAsync(_request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public async Task ApproveAsync_StoresNullComment_WhenBlank(string? comment)
    {
        var result = await CreateSut().ApproveAsync(
            500,
            new ApproveLeaveRequestRequestModel { Comment = comment },
            CancellationToken.None);

        Assert.That(result.DecisionComment, Is.Null);
    }

    [Test]
    public async Task ApproveAsync_Allows_ExactRemainingBalance()
    {
        _request.RequestedAmount = 4; // available = 5 - 1

        await CreateSut().ApproveAsync(500, new ApproveLeaveRequestRequestModel(), CancellationToken.None);

        Assert.That(_balance!.UsedAmount, Is.EqualTo(5m));
    }

    [Test]
    public void ApproveAsync_Throws_WhenInsufficientBalance_AndChangesNothing()
    {
        _balance!.UsedAmount = 4; // available = 1, requested = 2

        var ex = Assert.ThrowsAsync<BusinessRuleException>(() =>
            CreateSut().ApproveAsync(500, new ApproveLeaveRequestRequestModel(), CancellationToken.None));

        Assert.That(ex!.Message, Is.EqualTo(LeaveApprovalMessages.InsufficientBalance));
        AssertNothingChanged(expectedUsed: 4);
    }

    [Test]
    public void ApproveAsync_Throws_WhenBalanceRowMissing()
    {
        _balance = null;

        var ex = Assert.ThrowsAsync<BusinessRuleException>(() =>
            CreateSut().ApproveAsync(500, new ApproveLeaveRequestRequestModel(), CancellationToken.None));

        Assert.That(ex!.Message, Is.EqualTo(LeaveApprovalMessages.BalanceMissing));
        Assert.That(ex.Message, Does.Not.Contain("not found").IgnoreCase, "must map to 400, not 404");
        Assert.That(_request.Status, Is.EqualTo(LeaveRequestStatus.Pending));
        _requests.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void ApproveAsync_Throws_WhenCommentTooLong()
    {
        var ex = Assert.ThrowsAsync<BusinessRuleException>(() =>
            CreateSut().ApproveAsync(
                500,
                new ApproveLeaveRequestRequestModel { Comment = new string('x', 1001) },
                CancellationToken.None));

        Assert.That(ex!.Message, Is.EqualTo(LeaveApprovalMessages.CommentTooLong));
        AssertNothingChanged(expectedUsed: 1);
    }

    // ---------- Reject ----------

    [Test]
    public async Task RejectAsync_RejectsWithReason_AndLeavesBalanceUnchanged()
    {
        var result = await CreateSut().RejectAsync(
            500,
            new RejectLeaveRequestRequestModel { Reason = "  Short-staffed that week  " },
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(LeaveRequestStatus.Rejected));
            Assert.That(result.ReviewedByEmployeeId, Is.EqualTo(ReviewerId));
            Assert.That(result.ReviewedAtUtc, Is.Not.Null);
            Assert.That(result.DecisionComment, Is.EqualTo("Short-staffed that week"));
            Assert.That(_balance!.UsedAmount, Is.EqualTo(1m));
        });
        _balances.Verify(x => x.Update(It.IsAny<LeaveBalance>()), Times.Never);
        _requests.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _notifications.Verify(x => x.NotifyLeaveRejectedAsync(_request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void RejectAsync_Throws_WhenReasonMissing(string? reason)
    {
        var ex = Assert.ThrowsAsync<BusinessRuleException>(() =>
            CreateSut().RejectAsync(500, new RejectLeaveRequestRequestModel { Reason = reason }, CancellationToken.None));

        Assert.That(ex!.Message, Is.EqualTo(LeaveApprovalMessages.RejectionReasonRequired));
        AssertNothingChanged(expectedUsed: 1);
    }

    [Test]
    public void RejectAsync_Throws_WhenReasonTooLong()
    {
        var ex = Assert.ThrowsAsync<BusinessRuleException>(() =>
            CreateSut().RejectAsync(
                500,
                new RejectLeaveRequestRequestModel { Reason = new string('x', 1001) },
                CancellationToken.None));

        Assert.That(ex!.Message, Is.EqualTo(LeaveApprovalMessages.CommentTooLong));
        AssertNothingChanged(expectedUsed: 1);
    }

    // ---------- Shared rules ----------

    [TestCase(LeaveRequestStatus.Approved)]
    [TestCase(LeaveRequestStatus.Rejected)]
    [TestCase(LeaveRequestStatus.Cancelled)]
    public void ApproveAndReject_Throw_WhenNotPending(LeaveRequestStatus status)
    {
        _request.Status = status;
        var sut = CreateSut();

        var approveEx = Assert.ThrowsAsync<BusinessRuleException>(() =>
            sut.ApproveAsync(500, new ApproveLeaveRequestRequestModel(), CancellationToken.None));
        var rejectEx = Assert.ThrowsAsync<BusinessRuleException>(() =>
            sut.RejectAsync(500, new RejectLeaveRequestRequestModel { Reason = "No" }, CancellationToken.None));

        Assert.That(approveEx!.Message, Is.EqualTo(LeaveApprovalMessages.NotDecidable));
        Assert.That(rejectEx!.Message, Is.EqualTo(LeaveApprovalMessages.NotDecidable));
        Assert.That(_request.Status, Is.EqualTo(status));
        Assert.That(_balance!.UsedAmount, Is.EqualTo(1m));
        _requests.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestCase(LeaveApprovalMessages.Denied)]
    [TestCase(LeaveApprovalMessages.SelfDecisionDenied)]
    [TestCase(LeaveApprovalMessages.ReviewerNotLinked)]
    public void ApproveAndReject_PropagateAccessDenial_AndChangeNothing(string message)
    {
        _access.Setup(x => x.EnsureCanDecideAsync(It.IsAny<LeaveRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new BusinessRuleException(message));
        var sut = CreateSut();

        var approveEx = Assert.ThrowsAsync<BusinessRuleException>(() =>
            sut.ApproveAsync(500, new ApproveLeaveRequestRequestModel(), CancellationToken.None));
        var rejectEx = Assert.ThrowsAsync<BusinessRuleException>(() =>
            sut.RejectAsync(500, new RejectLeaveRequestRequestModel { Reason = "No" }, CancellationToken.None));

        Assert.That(approveEx!.Message, Is.EqualTo(message));
        Assert.That(rejectEx!.Message, Is.EqualTo(message));
        Assert.That(LeaveApprovalMessages.IsAccessDenied(message), Is.True);
        AssertNothingChanged(expectedUsed: 1);
    }

    [Test]
    public void ApproveAsync_Throws_NotFound_WhenRequestMissing()
    {
        var ex = Assert.ThrowsAsync<BusinessRuleException>(() =>
            CreateSut().ApproveAsync(999, new ApproveLeaveRequestRequestModel(), CancellationToken.None));

        Assert.That(ex!.Message, Is.EqualTo("Leave request was not found."));
    }

    [Test]
    public void ApproveAsync_MapsConcurrencyConflict_AndDoesNotNotify()
    {
        _requests.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException("Row version mismatch."));

        var ex = Assert.ThrowsAsync<BusinessRuleException>(() =>
            CreateSut().ApproveAsync(500, new ApproveLeaveRequestRequestModel(), CancellationToken.None));

        Assert.That(ex!.Message, Is.EqualTo(LeaveApprovalMessages.ConcurrencyConflict));
        _notifications.Verify(
            x => x.NotifyLeaveApprovedAsync(It.IsAny<LeaveRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public void RejectAsync_MapsConcurrencyConflict()
    {
        _requests.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException("Row version mismatch."));

        var ex = Assert.ThrowsAsync<BusinessRuleException>(() =>
            CreateSut().RejectAsync(500, new RejectLeaveRequestRequestModel { Reason = "No" }, CancellationToken.None));

        Assert.That(ex!.Message, Is.EqualTo(LeaveApprovalMessages.ConcurrencyConflict));
    }

    // ---------- Queue ----------

    [Test]
    public async Task GetManagerQueueAsync_ReturnsPendingRequestsOfDirectReports_OrderedByStartDate()
    {
        _managerTeamAccess
            .Setup(x => x.ResolveEffectiveManagerIdAsync(1, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ReviewerId);

        _employees.Setup(x => x.GetQueryable()).Returns(() => new List<Employee>
        {
            new() { Id = 20, OrganizationId = 1, ManagerId = ReviewerId, FirstName = "Alex", LastName = "Lee", EmployeeNumber = "E020" },
            new() { Id = 21, OrganizationId = 1, ManagerId = ReviewerId, FirstName = "Sam", LastName = "Ng", EmployeeNumber = "E021" },
            new() { Id = 22, OrganizationId = 1, ManagerId = ReviewerId, IsArchived = true },
            new() { Id = 30, OrganizationId = 1, ManagerId = 99 },
        }.BuildMock());

        _requests.Setup(x => x.GetQueryable()).Returns(() => new List<LeaveRequest>
        {
            new() { Id = 1, OrganizationId = 1, EmployeeId = 20, LeaveTypeId = 3, StartDateUtc = new DateTime(2026, 12, 10), Status = LeaveRequestStatus.Pending, RequestedAmount = 2, Reason = "Trip" },
            new() { Id = 2, OrganizationId = 1, EmployeeId = 21, LeaveTypeId = 3, StartDateUtc = new DateTime(2026, 12, 1), Status = LeaveRequestStatus.ModifiedPending, RequestedAmount = 1 },
            new() { Id = 3, OrganizationId = 1, EmployeeId = 20, LeaveTypeId = 3, StartDateUtc = new DateTime(2026, 11, 1), Status = LeaveRequestStatus.Approved },
            new() { Id = 4, OrganizationId = 1, EmployeeId = 20, LeaveTypeId = 3, StartDateUtc = new DateTime(2026, 11, 2), Status = LeaveRequestStatus.Rejected },
            new() { Id = 5, OrganizationId = 1, EmployeeId = 20, LeaveTypeId = 3, StartDateUtc = new DateTime(2026, 11, 3), Status = LeaveRequestStatus.Cancelled },
            new() { Id = 6, OrganizationId = 1, EmployeeId = 22, LeaveTypeId = 3, StartDateUtc = new DateTime(2026, 11, 4), Status = LeaveRequestStatus.Pending },
            new() { Id = 7, OrganizationId = 1, EmployeeId = 30, LeaveTypeId = 3, StartDateUtc = new DateTime(2026, 11, 5), Status = LeaveRequestStatus.Pending },
        }.BuildMock());

        _leaveTypes.Setup(x => x.GetQueryable()).Returns(() => new List<LeaveType>
        {
            new() { Id = 3, OrganizationId = 1, Name = "Annual leave" },
        }.BuildMock());

        _balances.Setup(x => x.GetQueryable()).Returns(() => new List<LeaveBalance>
        {
            new() { EmployeeId = 20, LeaveTypeId = 3, OpeningBalance = 10, UsedAmount = 2.5m },
        }.BuildMock());

        var result = await CreateSut().GetManagerQueueAsync(
            new ManagerLeaveApprovalQueryModel { OrganizationId = 1 },
            CancellationToken.None);

        Assert.That(result.Select(x => x.LeaveRequestId), Is.EqualTo(new[] { 2, 1 }));
        var alex = result.Single(x => x.LeaveRequestId == 1);
        Assert.Multiple(() =>
        {
            Assert.That(alex.EmployeeName, Is.EqualTo("Alex Lee"));
            Assert.That(alex.LeaveTypeName, Is.EqualTo("Annual leave"));
            Assert.That(alex.Reason, Is.EqualTo("Trip"));
            Assert.That(alex.AvailableBalance, Is.EqualTo(7.5m));
            Assert.That(result.Single(x => x.LeaveRequestId == 2).AvailableBalance, Is.Null);
        });
    }

    [Test]
    public void GetManagerQueueAsync_PropagatesManagerAccessDenial()
    {
        _managerTeamAccess
            .Setup(x => x.ResolveEffectiveManagerIdAsync(It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new BusinessRuleException(ManagerTeamAccessMessages.Denied));

        var ex = Assert.ThrowsAsync<BusinessRuleException>(() =>
            CreateSut().GetManagerQueueAsync(new ManagerLeaveApprovalQueryModel { OrganizationId = 1 }, CancellationToken.None));

        Assert.That(ex!.Message, Is.EqualTo(ManagerTeamAccessMessages.Denied));
    }

    private void AssertNothingChanged(decimal expectedUsed)
    {
        Assert.Multiple(() =>
        {
            Assert.That(_request.Status, Is.EqualTo(LeaveRequestStatus.Pending));
            Assert.That(_request.ReviewedByEmployeeId, Is.Null);
            Assert.That(_request.ReviewedAtUtc, Is.Null);
            Assert.That(_request.DecisionComment, Is.Null);
            Assert.That(_balance!.UsedAmount, Is.EqualTo(expectedUsed));
        });
        _requests.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
