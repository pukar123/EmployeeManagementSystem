using EMS.Application.DTOs.Document;
using EMS.Application.Services.Documents;
using EMS.Application.UnitTests.Infrastructure;
using EMS.Domain.DbModels;
using EMS.Domain.Enums;
using EMS.Domain.Repositories.Interface;
using Moq;
using Pukar.Shared;

namespace EMS.Application.UnitTests.Documents;

/// <summary>
/// B09: <see cref="DocumentService"/> asks the document access service before every read and change, refuses
/// changes from non-admins without touching anything, validates paging, pages inside the allowed scope, and keeps
/// its existing validation messages.
/// </summary>
public class DocumentServiceAccessTests
{
    private const int EmployeeA = 10;
    private const int EmployeeB = 20;
    private const int TypeContract = 1;
    private const int TypePassport = 2;
    private const int DocOfA = 100;
    private const int DocOfB = 200;

    // ----- Changes are for document admins only -----

    [Test]
    public void Create_NonAdmin_IsDenied_AndWritesNothing()
    {
        var ctx = new Context(isDocumentAdmin: false);
        var before = ctx.Documents.Items.Count;

        AssertDenied(() => ctx.Sut.CreateAsync(
            EmployeeA, TypeContract, "New", null, null, "new.pdf", "application/pdf", "Employee/10/Documents/x.pdf",
            DocumentFileKind.Pdf, CancellationToken.None));

        Assert.Multiple(() =>
        {
            Assert.That(ctx.Documents.Items, Has.Count.EqualTo(before));
            Assert.That(ctx.Links.Items.Count(l => l.DocumentId == 0), Is.Zero);
        });
    }

    [Test]
    public void Update_NonAdmin_IsDenied_AndChangesNothing()
    {
        var ctx = new Context(isDocumentAdmin: false);

        AssertDenied(() => ctx.Sut.UpdateAsync(
            DocOfB,
            new UpdateDocumentRequestModel { Name = "Changed", DocumentTypeId = TypeContract, EmployeeId = EmployeeA },
            CancellationToken.None));

        var doc = ctx.Documents.Items.Single(d => d.Id == DocOfB);
        Assert.Multiple(() =>
        {
            Assert.That(doc.Name, Is.EqualTo($"Document {DocOfB}"));
            Assert.That(ctx.Links.Items.Single(l => l.DocumentId == DocOfB).EmployeeId, Is.EqualTo(EmployeeB));
            Assert.That(ctx.Links.Items.Single(l => l.DocumentId == DocOfB).IsActive, Is.True);
        });
    }

    [Test]
    public void Delete_NonAdmin_IsDenied_AndRemovesNothing()
    {
        var ctx = new Context(isDocumentAdmin: false);

        AssertDenied(() => ctx.Sut.DeleteAsync(DocOfB, CancellationToken.None));

        Assert.That(ctx.Documents.Items.Any(d => d.Id == DocOfB), Is.True);
        ctx.DocumentsMock.Verify(x => x.Remove(It.IsAny<Document>()), Times.Never);
    }

    [Test]
    public async Task Changes_DocumentAdmin_KeepExistingValidationMessages()
    {
        var ctx = new Context(isDocumentAdmin: true);

        var unknownType = Assert.ThrowsAsync<BusinessRuleException>(() => ctx.Sut.CreateAsync(
            EmployeeA, 999, "New", null, null, "new.pdf", "application/pdf", "k", DocumentFileKind.Pdf, CancellationToken.None));
        var unknownEmployee = Assert.ThrowsAsync<BusinessRuleException>(() => ctx.Sut.CreateAsync(
            999, TypeContract, "New", null, null, "new.pdf", "application/pdf", "k", DocumentFileKind.Pdf, CancellationToken.None));
        var updateUnknownType = Assert.ThrowsAsync<BusinessRuleException>(() => ctx.Sut.UpdateAsync(
            DocOfA, new UpdateDocumentRequestModel { Name = "x", DocumentTypeId = 999 }, CancellationToken.None));
        var updateUnknownEmployee = Assert.ThrowsAsync<BusinessRuleException>(() => ctx.Sut.UpdateAsync(
            DocOfA, new UpdateDocumentRequestModel { Name = "x", DocumentTypeId = TypeContract, EmployeeId = 999 }, CancellationToken.None));
        var updateMissing = await ctx.Sut.UpdateAsync(
            404, new UpdateDocumentRequestModel { Name = "x", DocumentTypeId = TypeContract }, CancellationToken.None);
        var deleteMissing = await ctx.Sut.DeleteAsync(404, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(unknownType!.Message, Is.EqualTo("Document type was not found."));
            Assert.That(unknownEmployee!.Message, Is.EqualTo("Employee was not found."));
            Assert.That(updateUnknownType!.Message, Is.EqualTo("Document type was not found."));
            Assert.That(updateUnknownEmployee!.Message, Is.EqualTo("Employee was not found."));
            Assert.That(updateMissing, Is.Null);
            Assert.That(deleteMissing, Is.False);
        });
    }

    // ----- Paged list -----

    [TestCase(0, 25, DocumentService.PageMessage)]
    [TestCase(-1, 25, DocumentService.PageMessage)]
    [TestCase(1, 0, DocumentService.PageSizeMessage)]
    [TestCase(1, 101, DocumentService.PageSizeMessage)]
    public void GetPage_InvalidPaging_IsRejected_BeforeAnyAccessCheckOrQuery(int page, int pageSize, string message)
    {
        var ctx = new Context(isDocumentAdmin: true);

        var ex = Assert.ThrowsAsync<BusinessRuleException>(() => ctx.Sut.GetPageAsync(
            new DocumentListQueryModel { Page = page, PageSize = pageSize }, CancellationToken.None));

        Assert.That(ex!.Message, Is.EqualTo(message));
        Assert.That(message, Is.EqualTo(page < 1 ? "Page must be 1 or greater." : "PageSize must be between 1 and 100."));
        ctx.DocumentsMock.Verify(x => x.GetQueryable(), Times.Never);
    }

    [Test]
    public void GetPage_ListAll_NonAdmin_IsDenied_BeforeTheRepositoryIsQueried()
    {
        var ctx = new Context(isDocumentAdmin: false);

        AssertDenied(() => ctx.Sut.GetPageAsync(new DocumentListQueryModel(), CancellationToken.None));

        ctx.DocumentsMock.Verify(x => x.GetQueryable(), Times.Never);
        ctx.Access.Verify(x => x.EnsureIsDocumentAdminAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void GetPage_ByEmployee_Refused_IsDenied_BeforeTheRepositoryIsQueried()
    {
        var ctx = new Context(isDocumentAdmin: false);
        ctx.Access.Setup(x => x.EnsureCanViewEmployeeDocumentsAsync(EmployeeB, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new BusinessRuleException(DocumentAccessMessages.Denied));

        AssertDenied(() => ctx.Sut.GetPageAsync(new DocumentListQueryModel { EmployeeId = EmployeeB }, CancellationToken.None));

        ctx.DocumentsMock.Verify(x => x.GetQueryable(), Times.Never);
    }

    [Test]
    public async Task GetPage_ByEmployee_PagesInsideTheScope_WithTheTypeFilter()
    {
        var ctx = new Context(isDocumentAdmin: false);

        var all = await ctx.Sut.GetPageAsync(new DocumentListQueryModel { EmployeeId = EmployeeA, PageSize = 2 }, CancellationToken.None);
        var second = await ctx.Sut.GetPageAsync(
            new DocumentListQueryModel { EmployeeId = EmployeeA, PageSize = 2, Page = 2 }, CancellationToken.None);
        var passports = await ctx.Sut.GetPageAsync(
            new DocumentListQueryModel { EmployeeId = EmployeeA, DocumentTypeId = TypePassport }, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(all.TotalCount, Is.EqualTo(3), "Only A's active documents are counted.");
            Assert.That(all.TotalPages, Is.EqualTo(2));
            Assert.That(all.Items.Select(i => i.Id), Is.EqualTo(new[] { DocOfA + 2, DocOfA + 1 }), "Newest change first.");
            Assert.That(second.Items.Select(i => i.Id), Is.EqualTo(new[] { DocOfA }));
            Assert.That(passports.Items.Select(i => i.Id), Is.EqualTo(new[] { DocOfA + 1 }));
            Assert.That(all.Items.All(i => i.EmployeeId == EmployeeA), Is.True);
            Assert.That(all.Items[0].EmployeeDisplayName, Is.EqualTo("Ann Able"));
        });
        ctx.Access.Verify(x => x.EnsureCanViewEmployeeDocumentsAsync(EmployeeA, It.IsAny<CancellationToken>()), Times.Exactly(3));
        ctx.Access.Verify(x => x.EnsureIsDocumentAdminAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task GetPage_ListAll_DocumentAdmin_IncludesUnownedDocuments_TieBreakOnId()
    {
        var ctx = new Context(isDocumentAdmin: true);

        var page = await ctx.Sut.GetPageAsync(new DocumentListQueryModel { PageSize = 100 }, CancellationToken.None);

        var unowned = page.Items.Single(i => i.Id == Context.Unowned);
        Assert.Multiple(() =>
        {
            Assert.That(page.TotalCount, Is.EqualTo(ctx.Documents.Items.Count));
            Assert.That(unowned.EmployeeId, Is.Null);
            Assert.That(unowned.EmployeeDisplayName, Is.Null);
            // DocOfB and Unowned share an UpdatedAtUtc; the higher id comes first.
            var ids = page.Items.Select(i => i.Id).ToList();
            Assert.That(ids.IndexOf(Context.Unowned), Is.LessThan(ids.IndexOf(DocOfB)));
        });
    }

    // ----- One document -----

    [Test]
    public async Task GetById_And_GetStoredFile_AskAccessFirst()
    {
        var ctx = new Context(isDocumentAdmin: false);
        ctx.Access.Setup(x => x.EnsureCanViewDocumentAsync(DocOfB, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new BusinessRuleException(DocumentAccessMessages.Denied));
        ctx.Access.Setup(x => x.EnsureCanViewDocumentAsync(404, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        AssertDenied(() => ctx.Sut.GetByIdAsync(DocOfB, CancellationToken.None));
        AssertDenied(() => ctx.Sut.GetStoredFileAsync(DocOfB, CancellationToken.None));
        ctx.DocumentsMock.Verify(x => x.GetQueryable(), Times.Never);

        var missing = await ctx.Sut.GetByIdAsync(404, CancellationToken.None);
        var missingFile = await ctx.Sut.GetStoredFileAsync(404, CancellationToken.None);
        var own = await ctx.Sut.GetByIdAsync(DocOfA, CancellationToken.None);
        var ownFile = await ctx.Sut.GetStoredFileAsync(DocOfA, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(missing, Is.Null);
            Assert.That(missingFile, Is.Null);
            Assert.That(own?.Id, Is.EqualTo(DocOfA));
            Assert.That(own?.DownloadUrl, Is.EqualTo($"/api/Documents/{DocOfA}/file"));
            Assert.That(ownFile?.StoragePath, Is.EqualTo($"Employee/{EmployeeA}/Documents/{DocOfA}.pdf"));
        });
    }

    private static void AssertDenied(AsyncTestDelegate call)
    {
        var ex = Assert.ThrowsAsync<BusinessRuleException>(call);
        Assert.That(ex!.Message, Is.EqualTo(DocumentAccessMessages.Denied));
    }

    /// <summary>
    /// A's documents: DocOfA (contract), DocOfA+1 (passport), DocOfA+2 (contract), newest last, plus one with a
    /// deleted link to A. B's: DocOfB. And one with no owner.
    /// </summary>
    private sealed class Context
    {
        public const int DeletedLinkToA = 300;
        public const int Unowned = 400;

        private static readonly DateTime Base = new(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public Context(bool isDocumentAdmin)
        {
            var contract = new DocumentType { Id = TypeContract, Name = "Contract", IsActive = true };
            var passport = new DocumentType { Id = TypePassport, Name = "Passport", IsActive = true };
            var employeeA = new Employee { Id = EmployeeA, FirstName = "Ann", LastName = "Able" };
            var employeeB = new Employee { Id = EmployeeB, FirstName = "Ben", LastName = "Baker" };

            Types = new InMemoryRepositoryMock<DocumentType>(t => t.Id, (t, id) => t.Id = id);
            Types.Seed(contract, passport);
            Employees = new InMemoryRepositoryMock<Employee>(e => e.Id, (e, id) => e.Id = id);
            Employees.Seed(employeeA, employeeB);
            Documents = new InMemoryRepositoryMock<Document>(d => d.Id, (d, id) => d.Id = id);
            Links = new InMemoryRepositoryMock<EmployeeDocument>(l => l.Id, (l, id) => l.Id = id);

            Add(DocOfA, contract, employeeA, Base.AddMinutes(1));
            Add(DocOfA + 1, passport, employeeA, Base.AddMinutes(2));
            Add(DocOfA + 2, contract, employeeA, Base.AddMinutes(3));
            Add(DeletedLinkToA, contract, employeeA, Base.AddMinutes(4), linkDeleted: true);
            Add(DocOfB, contract, employeeB, Base.AddMinutes(5));
            Add(Unowned, contract, owner: null, Base.AddMinutes(5));

            Access = new Mock<IDocumentAccessService>();
            if (!isDocumentAdmin)
            {
                Access.Setup(x => x.EnsureIsDocumentAdminAsync(It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new BusinessRuleException(DocumentAccessMessages.Denied));
            }

            Access.Setup(x => x.EnsureCanViewDocumentAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((int id, CancellationToken _) => Documents.Items.Any(d => d.Id == id));

            DocumentsMock = Documents.CreateMock();
            Sut = new DocumentService(
                DocumentsMock.Object,
                Types.CreateMock().Object,
                Employees.CreateMock().Object,
                Links.CreateMock().Object,
                Access.Object);
        }

        public InMemoryRepositoryMock<Document> Documents { get; }
        public InMemoryRepositoryMock<DocumentType> Types { get; }
        public InMemoryRepositoryMock<Employee> Employees { get; }
        public InMemoryRepositoryMock<EmployeeDocument> Links { get; }
        public Mock<IBaseRepository<Document>> DocumentsMock { get; }
        public Mock<IDocumentAccessService> Access { get; }
        public DocumentService Sut { get; }

        private void Add(int id, DocumentType type, Employee? owner, DateTime updatedAtUtc, bool linkDeleted = false)
        {
            var document = new Document
            {
                Id = id,
                DocumentTypeId = type.Id,
                DocumentType = type,
                Name = $"Document {id}",
                FileKind = DocumentFileKind.Pdf,
                OriginalFileName = $"{id}.pdf",
                ContentType = "application/pdf",
                StoredRelativePath = $"Employee/{owner?.Id.ToString() ?? "unassigned"}/Documents/{id}.pdf",
                CreatedAtUtc = updatedAtUtc,
                UpdatedAtUtc = updatedAtUtc,
            };
            Documents.Seed(document);

            if (owner is null)
                return;

            var link = new EmployeeDocument
            {
                Id = id,
                DocumentId = id,
                Document = document,
                EmployeeId = owner.Id,
                Employee = owner,
                IsActive = true,
                IsDeleted = linkDeleted,
            };
            document.EmployeeDocuments.Add(link);
            Links.Seed(link);
        }
    }
}
