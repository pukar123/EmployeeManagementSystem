using EMS.Application.Services.Leave;
using EMS.Domain.DbModels;
using EMS.Domain.Repositories.Interface;
using Moq;
using Pukar.Shared;

namespace EMS.Application.UnitTests;

public class LeavePolicyRuleServiceTests
{
    [Test]
    public async Task DeleteAsync_RemovesPolicyRule_WhenItExists()
    {
        var policyRuleRepo = new Mock<ILeavePolicyRuleRepository>();
        var leaveTypeRepo = new Mock<ILeaveTypeRepository>();
        var entity = new LeavePolicyRule { Id = 5, LeaveTypeId = 3, OrganizationId = 1 };

        policyRuleRepo.Setup(x => x.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        policyRuleRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var sut = new LeavePolicyRuleService(policyRuleRepo.Object, leaveTypeRepo.Object);

        var deleted = await sut.DeleteAsync(5, CancellationToken.None);

        Assert.That(deleted, Is.True);
        policyRuleRepo.Verify(x => x.Remove(entity), Times.Once);
        policyRuleRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void DeleteAsync_Throws_WhenPolicyRuleMissing()
    {
        var policyRuleRepo = new Mock<ILeavePolicyRuleRepository>();
        var leaveTypeRepo = new Mock<ILeaveTypeRepository>();
        policyRuleRepo.Setup(x => x.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((LeavePolicyRule?)null);

        var sut = new LeavePolicyRuleService(policyRuleRepo.Object, leaveTypeRepo.Object);

        var ex = Assert.ThrowsAsync<BusinessRuleException>(async () => await sut.DeleteAsync(5, CancellationToken.None));
        Assert.That(ex!.Message, Is.EqualTo("Leave policy rule was not found."));
    }
}
