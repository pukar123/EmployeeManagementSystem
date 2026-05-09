using EMS.Application.DTOs.Leave;
using EMS.Application.Services.Leave;
using EMS.Domain.DbModels;
using EMS.Domain.Enums;
using EMS.Domain.Repositories.Interface;
using MockQueryable;
using Moq;
using Pukar.Shared;

namespace EMS.Application.UnitTests;

public class LeaveRequestServiceTests
{
    [Test]
    public void CreateAsync_Throws_WhenDateRangeOverlaps()
    {
        var requestRepo = new Mock<ILeaveRequestRepository>();
        var balanceRepo = new Mock<ILeaveBalanceRepository>();
        var leaveTypeRepo = new Mock<ILeaveTypeRepository>();
        var employeeRepo = new Mock<IBaseRepository<Employee>>();

        var existing = new List<LeaveRequest>
        {
            new()
            {
                Id = 5,
                EmployeeId = 10,
                LeaveTypeId = 3,
                StartDateUtc = new DateTime(2026, 5, 10),
                EndDateUtc = new DateTime(2026, 5, 12),
                Status = LeaveRequestStatus.Pending,
            },
        };

        requestRepo.Setup(x => x.GetQueryable()).Returns(existing.BuildMock());
        leaveTypeRepo.Setup(x => x.GetByIdAsync(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LeaveType { Id = 3, OrganizationId = 1, IsActive = true });
        employeeRepo.Setup(x => x.GetByIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Employee { Id = 10, OrganizationId = 1 });
        balanceRepo.Setup(x => x.GetByEmployeeAndTypeAsync(10, 3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LeaveBalance { EmployeeId = 10, LeaveTypeId = 3, OpeningBalance = 10 });

        var sut = new LeaveRequestService(
            requestRepo.Object,
            balanceRepo.Object,
            leaveTypeRepo.Object,
            employeeRepo.Object);

        var request = new CreateLeaveRequestRequestModel
        {
            EmployeeId = 10,
            LeaveTypeId = 3,
            StartDateUtc = new DateTime(2026, 5, 11),
            EndDateUtc = new DateTime(2026, 5, 15),
            RequestedAmount = 2,
        };

        var ex = Assert.ThrowsAsync<BusinessRuleException>(async () => await sut.CreateAsync(request, CancellationToken.None));
        Assert.That(ex!.Message, Is.EqualTo("Requested date range overlaps an existing leave request."));
    }

    [Test]
    public void CreateAsync_Throws_WhenInsufficientBalance()
    {
        var requestRepo = new Mock<ILeaveRequestRepository>();
        var balanceRepo = new Mock<ILeaveBalanceRepository>();
        var leaveTypeRepo = new Mock<ILeaveTypeRepository>();
        var employeeRepo = new Mock<IBaseRepository<Employee>>();

        requestRepo.Setup(x => x.GetQueryable()).Returns(new List<LeaveRequest>().BuildMock());
        leaveTypeRepo.Setup(x => x.GetByIdAsync(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LeaveType { Id = 3, OrganizationId = 1, IsActive = true });
        employeeRepo.Setup(x => x.GetByIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Employee { Id = 10, OrganizationId = 1 });
        balanceRepo.Setup(x => x.GetByEmployeeAndTypeAsync(10, 3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LeaveBalance
            {
                EmployeeId = 10,
                LeaveTypeId = 3,
                OpeningBalance = 1,
                AccruedAmount = 0,
                AdjustedAmount = 0,
                CarryForwardAmount = 0,
                UsedAmount = 0,
            });

        var sut = new LeaveRequestService(
            requestRepo.Object,
            balanceRepo.Object,
            leaveTypeRepo.Object,
            employeeRepo.Object);

        var request = new CreateLeaveRequestRequestModel
        {
            EmployeeId = 10,
            LeaveTypeId = 3,
            StartDateUtc = new DateTime(2026, 5, 20),
            EndDateUtc = new DateTime(2026, 5, 21),
            RequestedAmount = 2,
        };

        var ex = Assert.ThrowsAsync<BusinessRuleException>(async () => await sut.CreateAsync(request, CancellationToken.None));
        Assert.That(ex!.Message, Is.EqualTo("Insufficient leave balance for this request."));
    }

    [Test]
    public async Task GetAdminSummaryAsync_ReturnsExpectedCounts()
    {
        var requestRepo = new Mock<ILeaveRequestRepository>();
        var balanceRepo = new Mock<ILeaveBalanceRepository>();
        var leaveTypeRepo = new Mock<ILeaveTypeRepository>();
        var employeeRepo = new Mock<IBaseRepository<Employee>>();

        var rows = new List<LeaveRequest>
        {
            new() { Id = 1, OrganizationId = 9, EmployeeId = 11, LeaveTypeId = 1, StartDateUtc = new DateTime(2026, 5, 1), EndDateUtc = new DateTime(2026, 5, 4), Status = LeaveRequestStatus.Pending },
            new() { Id = 2, OrganizationId = 9, EmployeeId = 12, LeaveTypeId = 1, StartDateUtc = new DateTime(2026, 4, 29), EndDateUtc = new DateTime(2026, 5, 2), Status = LeaveRequestStatus.ModifiedPending },
            new() { Id = 3, OrganizationId = 9, EmployeeId = 13, LeaveTypeId = 1, StartDateUtc = new DateTime(2026, 5, 2), EndDateUtc = new DateTime(2026, 5, 6), Status = LeaveRequestStatus.Approved },
            new() { Id = 4, OrganizationId = 9, EmployeeId = 14, LeaveTypeId = 1, StartDateUtc = new DateTime(2026, 5, 1), EndDateUtc = new DateTime(2026, 5, 1), Status = LeaveRequestStatus.Cancelled },
            new() { Id = 5, OrganizationId = 9, EmployeeId = 15, LeaveTypeId = 1, StartDateUtc = new DateTime(2026, 4, 1), EndDateUtc = new DateTime(2026, 4, 2), Status = LeaveRequestStatus.Rejected },
            new() { Id = 6, OrganizationId = 77, EmployeeId = 16, LeaveTypeId = 1, StartDateUtc = new DateTime(2026, 5, 1), EndDateUtc = new DateTime(2026, 5, 2), Status = LeaveRequestStatus.Pending },
        };
        requestRepo.Setup(x => x.GetQueryable()).Returns(rows.BuildMock());

        var sut = new LeaveRequestService(
            requestRepo.Object,
            balanceRepo.Object,
            leaveTypeRepo.Object,
            employeeRepo.Object);

        var result = await sut.GetAdminSummaryAsync(9, new DateTime(2026, 5, 2), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.OrganizationId, Is.EqualTo(9));
            Assert.That(result.AppliedCount, Is.EqualTo(2));
            Assert.That(result.ApprovedCount, Is.EqualTo(1));
            Assert.That(result.RejectedCount, Is.EqualTo(1));
            Assert.That(result.CancelledCount, Is.EqualTo(1));
            Assert.That(result.CurrentlyOnLeaveCount, Is.EqualTo(3));
        });
    }

    [Test]
    public void GetAdminSummaryAsync_Throws_WhenOrganizationIdIsInvalid()
    {
        var requestRepo = new Mock<ILeaveRequestRepository>();
        var balanceRepo = new Mock<ILeaveBalanceRepository>();
        var leaveTypeRepo = new Mock<ILeaveTypeRepository>();
        var employeeRepo = new Mock<IBaseRepository<Employee>>();

        requestRepo.Setup(x => x.GetQueryable()).Returns(new List<LeaveRequest>().BuildMock());
        var sut = new LeaveRequestService(
            requestRepo.Object,
            balanceRepo.Object,
            leaveTypeRepo.Object,
            employeeRepo.Object);

        var ex = Assert.ThrowsAsync<BusinessRuleException>(async () =>
            await sut.GetAdminSummaryAsync(0, DateTime.UtcNow, CancellationToken.None));
        Assert.That(ex!.Message, Is.EqualTo("Organization id must be greater than zero."));
    }
}
