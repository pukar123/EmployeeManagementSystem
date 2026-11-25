using EMS.Application.Services.Documents;
using EMS.Application.Services.EmployeePortal;
using EMS.Application.Services.Manager;
using EMS.Application.UnitTests.Infrastructure;
using EMS.Domain.DbModels;
using Moq;
using Pukar.Shared;

namespace EMS.Application.UnitTests.Documents;

/// <summary>
/// B09: whose documents the current user may read (self, document admin, current direct manager with manager.team),
/// and how one document's owner is resolved (active, non-deleted link; otherwise no owner, document admins only).
/// </summary>
public class DocumentAccessServiceTests
{
    private const int SelfId = 10;
    private const int TargetId = 20;
    private const int MissingEmployeeId = 999;

    private const int OwnedByTarget = 1;
    private const int NoLink = 2;
    private const int DeletedLinkOnly = 3;
    private const int InactiveLinkOnly = 4;
    private const int OwnedBySelf = 5;
    private const int MissingDocumentId = 404;

    // ----- One employee's documents -----

    [Test]
    public void EmployeeDocuments_Self_IsAllowed()
    {
        var sut = CreateSut(linkedId: SelfId);

        Assert.DoesNotThrowAsync(() => sut.EnsureCanViewEmployeeDocumentsAsync(SelfId, CancellationToken.None));
    }

    [Test]
    public void EmployeeDocuments_DocumentAdmin_IsAllowed_EvenUnlinked_AndForAnUnknownEmployee()
    {
        var sut = CreateSut(linkedId: null, isDocumentAdmin: true);

        Assert.DoesNotThrowAsync(() => sut.EnsureCanViewEmployeeDocumentsAsync(TargetId, CancellationToken.None));
        Assert.DoesNotThrowAsync(() => sut.EnsureCanViewEmployeeDocumentsAsync(MissingEmployeeId, CancellationToken.None));
    }

    [Test]
    public void EmployeeDocuments_DirectManagerWithTeamMenu_IsAllowed()
    {
        var sut = CreateSut(linkedId: SelfId, canViewTeam: true, isDirectReport: true);

        Assert.DoesNotThrowAsync(() => sut.EnsureCanViewEmployeeDocumentsAsync(TargetId, CancellationToken.None));
    }

    [Test]
    public void EmployeeDocuments_ManagerWithoutTeamMenu_IsDenied()
    {
        var sut = CreateSut(linkedId: SelfId, canViewTeam: false, isDirectReport: true);

        AssertDenied(() => sut.EnsureCanViewEmployeeDocumentsAsync(TargetId, CancellationToken.None));
    }

    [Test]
    public void EmployeeDocuments_NotADirectReport_IsDenied()
    {
        var sut = CreateSut(linkedId: SelfId, canViewTeam: true, isDirectReport: false);

        AssertDenied(() => sut.EnsureCanViewEmployeeDocumentsAsync(TargetId, CancellationToken.None));
    }

    [Test]
    public void EmployeeDocuments_UnlinkedNonAdmin_IsDenied()
    {
        var sut = CreateSut(linkedId: null);

        AssertDenied(() => sut.EnsureCanViewEmployeeDocumentsAsync(TargetId, CancellationToken.None));
    }

    [Test]
    public void EmployeeDocuments_UnknownEmployee_ForNonAdmin_IsDenied()
    {
        // The manager rule is asked for real: an id that does not exist is never a direct report.
        var sut = CreateSut(linkedId: SelfId, canViewTeam: true, isDirectReport: false);

        AssertDenied(() => sut.EnsureCanViewEmployeeDocumentsAsync(MissingEmployeeId, CancellationToken.None));
    }

    [Test]
    public void EnsureIsDocumentAdmin_NonAdmin_IsDenied_AndAdmin_IsAllowed()
    {
        AssertDenied(() => CreateSut(linkedId: SelfId).EnsureIsDocumentAdminAsync(CancellationToken.None));
        Assert.DoesNotThrowAsync(() => CreateSut(linkedId: null, isDocumentAdmin: true).EnsureIsDocumentAdminAsync(CancellationToken.None));
    }

    // ----- One document (owner resolution) -----

    [Test]
    public async Task Document_ActiveLink_IsThatOwner()
    {
        var self = CreateSut(linkedId: SelfId);
        var manager = CreateSut(linkedId: SelfId, canViewTeam: true, isDirectReport: true);

        var selfResult = await self.EnsureCanViewDocumentAsync(OwnedBySelf, CancellationToken.None);
        var managerResult = await manager.EnsureCanViewDocumentAsync(OwnedByTarget, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(selfResult, Is.True);
            Assert.That(managerResult, Is.True);
        });
        AssertDenied(() => self.EnsureCanViewDocumentAsync(OwnedByTarget, CancellationToken.None));
    }

    [TestCase(NoLink)]
    [TestCase(DeletedLinkOnly)]
    [TestCase(InactiveLinkOnly)]
    public async Task Document_WithNoActiveOwner_IsForDocumentAdminsOnly(int documentId)
    {
        // Self and a direct manager of the old owner are both refused: a deleted or inactive link is not ownership.
        AssertDenied(() => CreateSut(linkedId: TargetId).EnsureCanViewDocumentAsync(documentId, CancellationToken.None));
        AssertDenied(() => CreateSut(linkedId: SelfId, canViewTeam: true, isDirectReport: true)
            .EnsureCanViewDocumentAsync(documentId, CancellationToken.None));

        var admin = CreateSut(linkedId: null, isDocumentAdmin: true);
        Assert.That(await admin.EnsureCanViewDocumentAsync(documentId, CancellationToken.None), Is.True);
    }

    [Test]
    public async Task Document_Unknown_IsDeniedForNonAdmin_AndNotFoundForAdmin()
    {
        AssertDenied(() => CreateSut(linkedId: SelfId, canViewTeam: true, isDirectReport: true)
            .EnsureCanViewDocumentAsync(MissingDocumentId, CancellationToken.None));

        var admin = CreateSut(linkedId: null, isDocumentAdmin: true);
        Assert.That(await admin.EnsureCanViewDocumentAsync(MissingDocumentId, CancellationToken.None), Is.False);
    }

    private static void AssertDenied(AsyncTestDelegate call)
    {
        var ex = Assert.ThrowsAsync<BusinessRuleException>(call);
        Assert.That(ex!.Message, Is.EqualTo(DocumentAccessMessages.Denied));
    }

    private static InMemoryRepositoryMock<Document> Documents()
    {
        var documents = new InMemoryRepositoryMock<Document>(d => d.Id, (d, id) => d.Id = id);
        documents.Seed(
            WithLinks(OwnedByTarget, new EmployeeDocument { EmployeeId = TargetId, IsActive = true, IsDeleted = false }),
            WithLinks(NoLink),
            WithLinks(DeletedLinkOnly, new EmployeeDocument { EmployeeId = TargetId, IsActive = true, IsDeleted = true }),
            WithLinks(InactiveLinkOnly, new EmployeeDocument { EmployeeId = TargetId, IsActive = false, IsDeleted = false }),
            WithLinks(OwnedBySelf, new EmployeeDocument { EmployeeId = SelfId, IsActive = true, IsDeleted = false }));
        return documents;
    }

    private static Document WithLinks(int id, params EmployeeDocument[] links)
    {
        var document = new Document { Id = id, Name = $"Document {id}" };
        foreach (var link in links)
        {
            link.DocumentId = id;
            document.EmployeeDocuments.Add(link);
        }

        return document;
    }

    private static DocumentAccessService CreateSut(
        int? linkedId,
        bool isDocumentAdmin = false,
        bool canViewTeam = false,
        bool isDirectReport = false)
    {
        var linked = new Mock<ILinkedEmployeeService>();
        linked.Setup(x => x.TryGetLinkedEmployeeAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(linkedId is null ? null : new Employee { Id = linkedId.Value, OrganizationId = 1 });

        var documentAdmin = new Mock<IDocumentAdminAccessService>();
        documentAdmin.Setup(x => x.IsDocumentAdminAsync(It.IsAny<CancellationToken>())).ReturnsAsync(isDocumentAdmin);

        var managerTeam = new Mock<IManagerTeamAccessService>();
        managerTeam.Setup(x => x.CanViewTeamDashboardAsync(It.IsAny<CancellationToken>())).ReturnsAsync(canViewTeam);
        managerTeam
            .Setup(x => x.IsDirectReportAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int _, int employeeId, CancellationToken _) => isDirectReport && employeeId != MissingEmployeeId);

        return new DocumentAccessService(linked.Object, documentAdmin.Object, managerTeam.Object, Documents().CreateMock().Object);
    }
}
