using EMS.Application.Services.Leave;
using EMS.Domain.DbModels;
using EMS.Domain.Repositories.Interface;
using MockQueryable;
using Moq;
using Pukar.Shared;

namespace EMS.Application.UnitTests;

public class LeaveTypeServiceTests
{
    [Test]
    public async Task DeleteAsync_RemovesLeaveType_WhenNoReferencesExist()
    {
        var leaveTypeRepo = new Mock<ILeaveTypeRepository>();
        var policyRuleRepo = new Mock<ILeavePolicyRuleRepository>();
        var balanceRepo = new Mock<ILeaveBalanceRepository>();
        var requestRepo = new Mock<ILeaveRequestRepository>();

        var entity = new LeaveType { Id = 7, OrganizationId = 1, Name = "Annual", IsActive = true };
        leaveTypeRepo.Setup(x => x.GetByIdAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        policyRuleRepo.Setup(x => x.GetQueryable()).Returns(new List<LeavePolicyRule>().BuildMock());
        balanceRepo.Setup(x => x.GetQueryable()).Returns(new List<LeaveBalance>().BuildMock());
        requestRepo.Setup(x => x.GetQueryable()).Returns(new List<LeaveRequest>().BuildMock());
        leaveTypeRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var sut = new LeaveTypeService(
            leaveTypeRepo.Object,
            policyRuleRepo.Object,
            balanceRepo.Object,
            requestRepo.Object);

        var deleted = await sut.DeleteAsync(7, CancellationToken.None);

        Assert.That(deleted, Is.True);
        leaveTypeRepo.Verify(x => x.Remove(entity), Times.Once);
        leaveTypeRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void DeleteAsync_Throws_WhenReferencedByPolicyRule()
    {
        var leaveTypeRepo = new Mock<ILeaveTypeRepository>();
        var policyRuleRepo = new Mock<ILeavePolicyRuleRepository>();
        var balanceRepo = new Mock<ILeaveBalanceRepository>();
        var requestRepo = new Mock<ILeaveRequestRepository>();

        leaveTypeRepo.Setup(x => x.GetByIdAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LeaveType { Id = 7, OrganizationId = 1, Name = "Annual", IsActive = true });
        policyRuleRepo.Setup(x => x.GetQueryable()).Returns(new List<LeavePolicyRule>
        {
            new() { Id = 1, LeaveTypeId = 7, OrganizationId = 1 },
        }.BuildMock());
        balanceRepo.Setup(x => x.GetQueryable()).Returns(new List<LeaveBalance>().BuildMock());
        requestRepo.Setup(x => x.GetQueryable()).Returns(new List<LeaveRequest>().BuildMock());

        var sut = new LeaveTypeService(
            leaveTypeRepo.Object,
            policyRuleRepo.Object,
            balanceRepo.Object,
            requestRepo.Object);

        var ex = Assert.ThrowsAsync<BusinessRuleException>(async () => await sut.DeleteAsync(7, CancellationToken.None));
        Assert.That(ex!.Message, Is.EqualTo("Leave type cannot be deleted because policy rules reference it."));
    }

    [Test]
    public void DeleteAsync_Throws_WhenReferencedByBalance()
    {
        var leaveTypeRepo = new Mock<ILeaveTypeRepository>();
        var policyRuleRepo = new Mock<ILeavePolicyRuleRepository>();
        var balanceRepo = new Mock<ILeaveBalanceRepository>();
        var requestRepo = new Mock<ILeaveRequestRepository>();

        leaveTypeRepo.Setup(x => x.GetByIdAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LeaveType { Id = 7, OrganizationId = 1, Name = "Annual", IsActive = true });
        policyRuleRepo.Setup(x => x.GetQueryable()).Returns(new List<LeavePolicyRule>().BuildMock());
        balanceRepo.Setup(x => x.GetQueryable()).Returns(new List<LeaveBalance>
        {
            new() { Id = 2, LeaveTypeId = 7, EmployeeId = 11 },
        }.BuildMock());
        requestRepo.Setup(x => x.GetQueryable()).Returns(new List<LeaveRequest>().BuildMock());

        var sut = new LeaveTypeService(
            leaveTypeRepo.Object,
            policyRuleRepo.Object,
            balanceRepo.Object,
            requestRepo.Object);

        var ex = Assert.ThrowsAsync<BusinessRuleException>(async () => await sut.DeleteAsync(7, CancellationToken.None));
        Assert.That(ex!.Message, Is.EqualTo("Leave type cannot be deleted because leave balances reference it."));
    }

    [Test]
    public void DeleteAsync_Throws_WhenReferencedByRequest()
    {
        var leaveTypeRepo = new Mock<ILeaveTypeRepository>();
        var policyRuleRepo = new Mock<ILeavePolicyRuleRepository>();
        var balanceRepo = new Mock<ILeaveBalanceRepository>();
        var requestRepo = new Mock<ILeaveRequestRepository>();

        leaveTypeRepo.Setup(x => x.GetByIdAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LeaveType { Id = 7, OrganizationId = 1, Name = "Annual", IsActive = true });
        policyRuleRepo.Setup(x => x.GetQueryable()).Returns(new List<LeavePolicyRule>().BuildMock());
        balanceRepo.Setup(x => x.GetQueryable()).Returns(new List<LeaveBalance>().BuildMock());
        requestRepo.Setup(x => x.GetQueryable()).Returns(new List<LeaveRequest>
        {
            new() { Id = 3, LeaveTypeId = 7, EmployeeId = 11 },
        }.BuildMock());

        var sut = new LeaveTypeService(
            leaveTypeRepo.Object,
            policyRuleRepo.Object,
            balanceRepo.Object,
            requestRepo.Object);

        var ex = Assert.ThrowsAsync<BusinessRuleException>(async () => await sut.DeleteAsync(7, CancellationToken.None));
        Assert.That(ex!.Message, Is.EqualTo("Leave type cannot be deleted because leave requests reference it."));
    }
}
