using EMS.Application.Services.Authorization;
using EMS.Application.Services.Employees;
using Moq;
using Pukar.Shared;

namespace EMS.Application.UnitTests.Employees;

public class EmployeeAccessServiceTests
{
    [Test]
    public void EnsureCanViewEmployeesAsync_Throws_WhenNoPermission()
    {
        var identity = new Mock<IIdentityContext>();
        identity.Setup(x => x.GetCurrent()).Returns(new UserIdentitySnapshot { RoleKeys = ["HR"] });

        var permissions = new Mock<IPermissionEvaluator>();
        permissions
            .Setup(x => x.HasCapabilityAsync(It.IsAny<IReadOnlyList<string>>(), EmployeeCapabilities.View, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var sut = new EmployeeAccessService(identity.Object, permissions.Object);

        var ex = Assert.ThrowsAsync<BusinessRuleException>(() => sut.EnsureCanViewEmployeesAsync(CancellationToken.None));
        Assert.That(ex!.Message, Is.EqualTo(EmployeeAccessMessages.Denied));
    }

    [Test]
    public async Task EnsureCanViewEmployeesAsync_Allows_WhenViewCapabilityGranted()
    {
        var identity = new Mock<IIdentityContext>();
        identity.Setup(x => x.GetCurrent()).Returns(new UserIdentitySnapshot { RoleKeys = ["HR"] });

        var permissions = new Mock<IPermissionEvaluator>();
        permissions
            .Setup(x => x.HasCapabilityAsync(It.IsAny<IReadOnlyList<string>>(), EmployeeCapabilities.View, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sut = new EmployeeAccessService(identity.Object, permissions.Object);

        Assert.DoesNotThrowAsync(() => sut.EnsureCanViewEmployeesAsync(CancellationToken.None));
    }

    [Test]
    public void EnsureCanManageEmployeesAsync_Throws_WhenOnlyViewGranted()
    {
        var identity = new Mock<IIdentityContext>();
        identity.Setup(x => x.GetCurrent()).Returns(new UserIdentitySnapshot { RoleKeys = ["HR"] });

        var permissions = new Mock<IPermissionEvaluator>();
        permissions
            .Setup(x => x.HasCapabilityAsync(It.IsAny<IReadOnlyList<string>>(), EmployeeCapabilities.View, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        permissions
            .Setup(x => x.HasCapabilityAsync(It.IsAny<IReadOnlyList<string>>(), EmployeeCapabilities.Manage, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var sut = new EmployeeAccessService(identity.Object, permissions.Object);

        var ex = Assert.ThrowsAsync<BusinessRuleException>(() => sut.EnsureCanManageEmployeesAsync(CancellationToken.None));
        Assert.That(ex!.Message, Is.EqualTo(EmployeeAccessMessages.Denied));
    }

    [Test]
    public async Task EnsureCanManageEmployeesAsync_Allows_AdminBypass()
    {
        var identity = new Mock<IIdentityContext>();
        identity.Setup(x => x.GetCurrent()).Returns(new UserIdentitySnapshot { RoleKeys = ["ADMIN"] });

        var permissions = new Mock<IPermissionEvaluator>();

        var sut = new EmployeeAccessService(identity.Object, permissions.Object);

        Assert.DoesNotThrowAsync(() => sut.EnsureCanManageEmployeesAsync(CancellationToken.None));
    }

    [Test]
    public async Task GetMyCapabilitiesAsync_ReturnsAllForAdmin()
    {
        var identity = new Mock<IIdentityContext>();
        identity.Setup(x => x.GetCurrent()).Returns(new UserIdentitySnapshot { RoleKeys = ["ADMIN"] });

        var permissions = new Mock<IPermissionEvaluator>();
        var sut = new EmployeeAccessService(identity.Object, permissions.Object);

        var caps = await sut.GetMyCapabilitiesAsync(CancellationToken.None);

        Assert.That(caps.View, Is.True);
        Assert.That(caps.Manage, Is.True);
        Assert.That(caps.Access, Is.True);
        Assert.That(caps.Export, Is.True);
    }
}
