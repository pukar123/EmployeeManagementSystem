using EMS.Application.Services.Authorization;
using EMS.Application.Services.Documents;
using EMS.Domain.DbModels;
using EMS.Domain.Repositories.Interface;
using MockQueryable;
using Moq;

namespace EMS.Application.UnitTests.Documents;

/// <summary>B09: the single definition of "document admin" (admin, or a role granted the documents.admin menu).</summary>
public class DocumentAdminAccessServiceTests
{
    private const int DocumentAdminMenuId = 77;
    private const int EmployeesMenuId = 10;

    [TestCase("ADMIN")]
    [TestCase("ADMINISTRATOR")]
    public async Task IsDocumentAdminAsync_ReturnsTrue_ForAdminRoles_WithoutAskingTheEvaluator(string roleKey)
    {
        var (sut, permissions, menus) = CreateSut([roleKey]);

        var result = await sut.IsDocumentAdminAsync(CancellationToken.None);

        Assert.That(result, Is.True);
        menus.Verify(x => x.GetQueryable(), Times.Never);
        permissions.Verify(
            x => x.IsMenuAllowedAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task IsDocumentAdminAsync_ReturnsTrue_ForRoleGrantedDocumentAdminMenu()
    {
        var (sut, _, _) = CreateSut(["DOCUMENTOFFICER"], documentAdminAllowed: true);

        Assert.That(await sut.IsDocumentAdminAsync(CancellationToken.None), Is.True);
    }

    [Test]
    public async Task IsDocumentAdminAsync_ReturnsFalse_ForDirectoryRightsOnly()
    {
        // The employees menu is allowed here (as for a role with employees.view and employees.manage), to prove it
        // does not count. Capabilities are never asked.
        var (sut, permissions, _) = CreateSut(["DIRECTORYEDITOR"], documentAdminAllowed: false);

        var result = await sut.IsDocumentAdminAsync(CancellationToken.None);

        Assert.That(result, Is.False);
        permissions.Verify(
            x => x.IsMenuAllowedAsync(It.IsAny<IReadOnlyList<string>>(), DocumentAdminMenuId, It.IsAny<CancellationToken>()),
            Times.Once);
        permissions.Verify(
            x => x.IsMenuAllowedAsync(It.IsAny<IReadOnlyList<string>>(), EmployeesMenuId, It.IsAny<CancellationToken>()),
            Times.Never);
        permissions.Verify(
            x => x.HasCapabilityAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task IsDocumentAdminAsync_ReturnsFalse_WithNoRoleKeys()
    {
        var (sut, _, _) = CreateSut([], documentAdminAllowed: true);

        Assert.That(await sut.IsDocumentAdminAsync(CancellationToken.None), Is.False);
    }

    [Test]
    public async Task IsDocumentAdminAsync_WhenDocumentAdminMenuIsMissing_ReturnsFalseForNonAdmin()
    {
        var (sut, _, _) = CreateSut(["DOCUMENTOFFICER"], documentAdminAllowed: true, includeDocumentAdminMenu: false);

        Assert.That(await sut.IsDocumentAdminAsync(CancellationToken.None), Is.False);
    }

    [Test]
    public async Task IsDocumentAdminAsync_IsCached_SecondCallDoesNotAskAgain()
    {
        var (sut, permissions, menus) = CreateSut(["DOCUMENTOFFICER"], documentAdminAllowed: true);

        var first = await sut.IsDocumentAdminAsync(CancellationToken.None);
        var second = await sut.IsDocumentAdminAsync(CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.True);
            Assert.That(second, Is.True);
        });
        menus.Verify(x => x.GetQueryable(), Times.Once);
        permissions.Verify(
            x => x.IsMenuAllowedAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    internal static (DocumentAdminAccessService Sut, Mock<IPermissionEvaluator> Permissions, Mock<IBaseRepository<Menu>> Menus) CreateSut(
        string[] roleKeys,
        bool documentAdminAllowed = false,
        bool includeDocumentAdminMenu = true)
    {
        var identity = new Mock<IIdentityContext>();
        identity.Setup(x => x.GetCurrent()).Returns(new UserIdentitySnapshot { RoleKeys = roleKeys.ToList() });

        var permissions = new Mock<IPermissionEvaluator>();
        permissions
            .Setup(x => x.IsMenuAllowedAsync(It.IsAny<IReadOnlyList<string>>(), DocumentAdminMenuId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(documentAdminAllowed);
        permissions
            .Setup(x => x.IsMenuAllowedAsync(It.IsAny<IReadOnlyList<string>>(), EmployeesMenuId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var menuRows = new List<Menu> { new() { Id = EmployeesMenuId, Key = "employees" } };
        if (includeDocumentAdminMenu)
            menuRows.Add(new Menu { Id = DocumentAdminMenuId, Key = "documents.admin" });

        var menus = new Mock<IBaseRepository<Menu>>();
        menus.Setup(x => x.GetQueryable()).Returns(() => menuRows.BuildMock());

        return (new DocumentAdminAccessService(identity.Object, permissions.Object, menus.Object), permissions, menus);
    }
}
