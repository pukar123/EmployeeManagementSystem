using EMS.Application.Services.Authorization;
using EMS.Application.Services.Employees;
using EMS.Domain.DbModels;
using EMS.Domain.Repositories.Interface;
using MockQueryable;
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

        var menus = new List<Menu> { new() { Id = 5, Key = "employees" } };
        var menuRepo = new Mock<IBaseRepository<Menu>>();
        menuRepo.Setup(x => x.GetQueryable()).Returns(menus.BuildMock());

        var permissions = new Mock<IPermissionEvaluator>();
        permissions
            .Setup(x => x.IsMenuAllowedAsync(It.IsAny<IReadOnlyList<string>>(), 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var sut = new EmployeeAccessService(identity.Object, permissions.Object, menuRepo.Object);

        var ex = Assert.ThrowsAsync<BusinessRuleException>(() => sut.EnsureCanViewEmployeesAsync(CancellationToken.None));
        Assert.That(ex!.Message, Is.EqualTo(EmployeeAccessMessages.Denied));
    }

    [Test]
    public async Task EnsureCanViewEmployeesAsync_Allows_WhenEmployeesMenuGranted()
    {
        var identity = new Mock<IIdentityContext>();
        identity.Setup(x => x.GetCurrent()).Returns(new UserIdentitySnapshot { RoleKeys = ["HR"] });

        var menus = new List<Menu> { new() { Id = 5, Key = "employees" } };
        var menuRepo = new Mock<IBaseRepository<Menu>>();
        menuRepo.Setup(x => x.GetQueryable()).Returns(menus.BuildMock());

        var permissions = new Mock<IPermissionEvaluator>();
        permissions
            .Setup(x => x.IsMenuAllowedAsync(It.IsAny<IReadOnlyList<string>>(), 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sut = new EmployeeAccessService(identity.Object, permissions.Object, menuRepo.Object);

        Assert.DoesNotThrowAsync(() => sut.EnsureCanViewEmployeesAsync(CancellationToken.None));
    }

    [Test]
    public async Task EnsureCanManageEmployeesAsync_Allows_AdminBypass()
    {
        var identity = new Mock<IIdentityContext>();
        identity.Setup(x => x.GetCurrent()).Returns(new UserIdentitySnapshot { RoleKeys = ["ADMIN"] });

        var menuRepo = new Mock<IBaseRepository<Menu>>();
        var permissions = new Mock<IPermissionEvaluator>();

        var sut = new EmployeeAccessService(identity.Object, permissions.Object, menuRepo.Object);

        Assert.DoesNotThrowAsync(() => sut.EnsureCanManageEmployeesAsync(CancellationToken.None));
    }
}
