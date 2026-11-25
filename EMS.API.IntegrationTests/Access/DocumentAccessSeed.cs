using System.Globalization;
using EMS.API.IntegrationTests.Harness;
using EMS.Domain.Database;
using EMS.Domain.DbModels;
using EMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EMS.API.IntegrationTests.Access;

/// <summary>
/// Extra rows for <see cref="DocumentAccessTests"/> (B09), added on top of <see cref="TestSeed"/> and never changing
/// O1, M1, E1 or E2. Ids are fixed in the 87xx range, which no other seed uses, so cases can name them.
/// <list type="bullet">
/// <item>Role permissions: <c>DOCUMENTOFFICER</c> gets only the <c>documents.admin</c> menu; <c>DIRECTORYEDITOR</c> gets
/// the <c>employees</c> menu and the <c>employees.view</c> and <c>employees.manage</c> capabilities, and nothing for
/// documents. Menu ids are looked up by key.</item>
/// <item>Document types T1 "Contract" and T2 "Passport".</item>
/// <item>Documents 8701 to 8706 (owners E1, E1, E2, M1, none, and E1 through a deleted link, which counts as no owner),
/// each with a file in the private root holding <see cref="ContentOf"/>.</item>
/// <item>8711 to 8715, owned by E2, one per change case (denied update and delete, admin update and delete, admin edit
/// keeping the same owner).</item>
/// <item>P1, an employee in O1 with no manager, who owns the 30 paging documents 8731 to 8760, each with its own
/// <c>UpdatedAtUtc</c>.</item>
/// </list>
/// </summary>
public static class DocumentAccessSeed
{
    public const string DocumentOfficerRole = "DocumentOfficer";
    public const string DirectoryEditorRole = "DirectoryEditor";

    public const int TypeContract = 8701;
    public const int TypePassport = 8702;

    public const int DocE1Contract = 8701;
    public const int DocE1Passport = 8702;
    public const int DocE2Contract = 8703;
    public const int DocM1Contract = 8704;
    public const int DocNoOwner = 8705;
    public const int DocDeletedLink = 8706;

    public const int DocE2DeniedUpdate = 8711;
    public const int DocE2DeniedDelete = 8712;
    public const int DocE2AdminUpdate = 8713;
    public const int DocE2AdminDelete = 8714;

    /// <summary>Owned by E2; an admin edits it keeping the same owner (one link per employee and document).</summary>
    public const int DocE2SameOwnerEdit = 8715;

    public const int FirstPagingDoc = 8731;
    public const int PagingDocCount = 30;

    public static readonly SeededEmployee P1 = new("P1", EmployeeId: 8721, UserId: 8921, FirstName: "Pat", LastName: "Pages");

    /// <summary>Oldest paging document's <c>UpdatedAtUtc</c>; each later id is one minute newer.</summary>
    public static readonly DateTime PagingBaseTime = new(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static string ContentOf(int documentId)
        => $"B09 document {documentId.ToString(CultureInfo.InvariantCulture)} content";

    public static string StorageKeyOf(int documentId, int? ownerId)
    {
        var folder = ownerId is int id ? id.ToString(CultureInfo.InvariantCulture) : "unassigned";
        return $"Employee/{folder}/Documents/b09-{documentId.ToString(CultureInfo.InvariantCulture)}.pdf";
    }

    /// <summary>Adds the rows and writes the seeded files under <paramref name="privateRootPath"/>.</summary>
    internal static async Task AddRowsAsync(AppDbContext db, string privateRootPath)
    {
        var now = DateTime.UtcNow;
        var e1 = TestSeed.E1.EmployeeId;
        var e2 = TestSeed.E2.EmployeeId;
        var m1 = TestSeed.M1.EmployeeId;

        await GrantMenuAsync(db, DocumentOfficerRole, "documents.admin");
        await GrantMenuAsync(db, DirectoryEditorRole, "employees");
        GrantCapability(db, DirectoryEditorRole, "employees.view");
        GrantCapability(db, DirectoryEditorRole, "employees.manage");

        db.DocumentTypes.Add(new DocumentType { Id = TypeContract, Name = "Contract", IsActive = true });
        db.DocumentTypes.Add(new DocumentType { Id = TypePassport, Name = "Passport", IsActive = true });

        db.Employees.Add(NewEmployee(P1, now));

        var seeded = new (int Id, int? Owner, int Type, bool LinkDeleted)[]
        {
            (DocE1Contract, e1, TypeContract, false),
            (DocE1Passport, e1, TypePassport, false),
            (DocE2Contract, e2, TypeContract, false),
            (DocM1Contract, m1, TypeContract, false),
            (DocNoOwner, null, TypeContract, false),
            (DocDeletedLink, e1, TypeContract, true),
            (DocE2DeniedUpdate, e2, TypeContract, false),
            (DocE2DeniedDelete, e2, TypeContract, false),
            (DocE2AdminUpdate, e2, TypeContract, false),
            (DocE2AdminDelete, e2, TypeContract, false),
            (DocE2SameOwnerEdit, e2, TypeContract, false),
        };

        foreach (var (id, owner, type, linkDeleted) in seeded)
        {
            var fileOwner = linkDeleted ? null : owner;
            var key = StorageKeyOf(id, fileOwner);
            db.Documents.Add(NewDocument(id, type, key, now));
            if (owner is int ownerId)
                db.EmployeeDocuments.Add(NewLink(id, ownerId, isDeleted: linkDeleted, now));

            WriteFile(privateRootPath, key, ContentOf(id));
        }

        for (var i = 0; i < PagingDocCount; i++)
        {
            var id = FirstPagingDoc + i;
            var updated = PagingBaseTime.AddMinutes(i);
            db.Documents.Add(NewDocument(id, TypeContract, StorageKeyOf(id, P1.EmployeeId), updated));
            db.EmployeeDocuments.Add(NewLink(id, P1.EmployeeId, isDeleted: false, updated));
        }
    }

    private static Document NewDocument(int id, int typeId, string storageKey, DateTime at) => new()
    {
        Id = id,
        DocumentTypeId = typeId,
        Name = $"B09 document {id.ToString(CultureInfo.InvariantCulture)}",
        FileKind = DocumentFileKind.Pdf,
        OriginalFileName = $"b09-{id.ToString(CultureInfo.InvariantCulture)}.pdf",
        ContentType = "application/pdf",
        StoredRelativePath = storageKey,
        CreatedAtUtc = at,
        UpdatedAtUtc = at,
    };

    private static EmployeeDocument NewLink(int documentId, int employeeId, bool isDeleted, DateTime at) => new()
    {
        Id = documentId,
        DocumentId = documentId,
        EmployeeId = employeeId,
        IsActive = true,
        IsDeleted = isDeleted,
        CreatedAtUtc = at,
        UpdatedAtUtc = at,
    };

    private static Employee NewEmployee(SeededEmployee seeded, DateTime now) => new()
    {
        Id = seeded.EmployeeId,
        OrganizationId = TestSeed.O1.Id,
        ManagerId = null,
        EmployeeNumber = $"T-{seeded.Label}",
        FirstName = seeded.FirstName,
        LastName = seeded.LastName,
        Email = $"{seeded.Label.ToLowerInvariant()}@ems-tests.invalid",
        DateOfBirth = new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        DateJoined = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        EmploymentStatus = EmploymentStatus.Active,
        IsActive = true,
        IsArchived = false,
        ExternalIdentityKey = seeded.UserId.ToString(CultureInfo.InvariantCulture),
        CreatedAtUtc = now,
        UpdatedAtUtc = now,
    };

    private static void WriteFile(string privateRootPath, string key, string content)
    {
        var path = Path.Combine(privateRootPath, key.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
    }

    private static async Task GrantMenuAsync(AppDbContext db, string roleName, string menuKey)
    {
        var menuId = await db.Menus.AsNoTracking()
            .Where(m => m.Key == menuKey)
            .Select(m => m.Id)
            .SingleAsync();

        db.RoleKeyPermissions.Add(new RoleKeyPermission
        {
            RoleKey = roleName.ToUpperInvariant(),
            MenuId = menuId,
            Allowed = true,
        });
    }

    private static void GrantCapability(AppDbContext db, string roleName, string capabilityKey)
        => db.RoleKeyCapabilities.Add(new RoleKeyCapability
        {
            RoleKey = roleName.ToUpperInvariant(),
            CapabilityKey = capabilityKey,
            Allowed = true,
        });
}
