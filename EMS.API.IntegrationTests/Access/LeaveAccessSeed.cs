using EMS.API.IntegrationTests.Harness;
using EMS.Domain.Database;
using EMS.Domain.DbModels;
using EMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EMS.API.IntegrationTests.Access;

/// <summary>
/// Extra rows for <see cref="LeaveAccessTests"/> (B06), added on top of <see cref="TestSeed"/> and never changing
/// O1, M1, E1 or E2. Ids are fixed in a range the other seeds do not use, so cases can name them.
/// <list type="bullet">
/// <item>Role permissions: <c>LEAVEUSER</c> gets the <c>leave</c> menu, <c>LEAVEOFFICER</c> only <c>leave.admin</c>,
/// <c>LEAVESETTINGSONLY</c> only <c>leave.admin.settings</c>. Menu ids are looked up by key.</item>
/// <item>LT1, an active leave type in O1, and a balance of 100 days on it for M1, E1 and E2.</item>
/// <item>Pending requests on separate single days, one per case that changes a request (see the constants).</item>
/// <item>Attachments with a small real file each, written where the API stores and reads leave attachments
/// (see <see cref="AttachmentRoot"/>), so allowed downloads return 200.</item>
/// </list>
/// </summary>
public static class LeaveAccessSeed
{
    public const string LeaveUserRole = "LeaveUser";
    public const string LeaveOfficerRole = "LeaveOfficer";
    public const string LeaveSettingsOnlyRole = "LeaveSettingsOnly";

    public const int LeaveTypeId = 9601; // LT1

    public const int RequestE1Read = 9601;            // R_E1_Read
    public const int RequestE2Read = 9602;            // R_E2_Read
    public const int RequestE1CancelSelf = 9603;      // R_E1_Cancel_Self
    public const int RequestE1CancelOfficer = 9604;   // R_E1_Cancel_Officer
    public const int RequestE1CancelAdmin = 9605;     // R_E1_Cancel_Admin
    public const int RequestE1UpdateSelf = 9606;      // R_E1_Update_Self
    public const int RequestE1UpdateOfficer = 9607;   // R_E1_Update_Officer
    public const int RequestE1ApproveManager = 9608;  // R_E1_Approve_M1
    public const int RequestE1ApproveOfficer = 9609;  // R_E1_Approve_Officer
    public const int RequestE2Own = 9610;             // R_E2_Own

    public const int AttachmentE1 = 9701;              // A_E1 on R_E1_Read
    public const int AttachmentE2 = 9702;              // A_E2 on R_E2_Read
    public const int AttachmentE1DeleteSelf = 9703;    // deleted by E1 (B06-05)
    public const int AttachmentE1DeleteOfficer = 9704; // deleted by the leave officer (B06-12)

    /// <summary>Request ids whose attachment folders this fixture creates and removes again.</summary>
    public static readonly int[] RequestIds =
    [
        RequestE1Read, RequestE2Read, RequestE1CancelSelf, RequestE1CancelOfficer, RequestE1CancelAdmin,
        RequestE1UpdateSelf, RequestE1UpdateOfficer, RequestE1ApproveManager, RequestE1ApproveOfficer, RequestE2Own,
    ];

    /// <summary>
    /// The single day each seeded request covers. Requests on the same employee and leave type must not overlap,
    /// and the create cases use later days (see <see cref="FreeDay"/>).
    /// </summary>
    public static DateTime DayOf(int requestId)
        => new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(requestId - RequestE1Read);

    /// <summary>A day no seeded request uses, for create cases. Pass a different <paramref name="slot"/> per case.</summary>
    public static DateTime FreeDay(int slot)
        => new DateTime(2027, 3, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(slot);

    public static string AttachmentRelativePath(int requestId, int attachmentId)
        => $"/attachments/Leave/{requestId}/b06-{attachmentId}.txt";

    /// <summary>
    /// Where the API keeps leave attachment files: the web root, or <c>ContentRoot/wwwroot</c> when the test host
    /// has no web root (the same rule <c>LocalLeaveAttachmentStorage</c> and the download action use).
    /// </summary>
    public static string AttachmentRoot(string? webRootPath, string contentRootPath)
        => webRootPath ?? Path.Combine(contentRootPath, "wwwroot");

    public static string PhysicalPath(string webRoot, string relativePath)
        => Path.Combine(webRoot, relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

    internal static async Task AddRowsAsync(AppDbContext db, string webRoot)
    {
        var now = DateTime.UtcNow;
        var o1 = TestSeed.O1.Id;

        await GrantMenuAsync(db, LeaveUserRole, "leave");
        await GrantMenuAsync(db, LeaveOfficerRole, "leave.admin");
        await GrantMenuAsync(db, LeaveSettingsOnlyRole, "leave.admin.settings");

        db.LeaveTypes.Add(new LeaveType
        {
            Id = LeaveTypeId,
            OrganizationId = o1,
            Name = "B06 Annual Leave",
            Unit = LeaveUnit.Days,
            IsActive = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        });

        var balanceId = 9601;
        foreach (var employee in new[] { TestSeed.M1, TestSeed.E1, TestSeed.E2 })
        {
            db.LeaveBalances.Add(new LeaveBalance
            {
                Id = balanceId++,
                OrganizationId = o1,
                EmployeeId = employee.EmployeeId,
                LeaveTypeId = LeaveTypeId,
                OpeningBalance = 100m,
                BalanceAsOfUtc = now,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
            });
        }

        foreach (var requestId in RequestIds)
        {
            var owner = requestId is RequestE2Read or RequestE2Own ? TestSeed.E2 : TestSeed.E1;
            db.LeaveRequests.Add(new LeaveRequest
            {
                Id = requestId,
                OrganizationId = o1,
                EmployeeId = owner.EmployeeId,
                LeaveTypeId = LeaveTypeId,
                StartDateUtc = DayOf(requestId),
                EndDateUtc = DayOf(requestId),
                Unit = LeaveUnit.Days,
                RequestedAmount = 1m,
                Reason = "B06 seed",
                Status = LeaveRequestStatus.Pending,
                SubmittedAtUtc = now,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
            });
        }

        AddAttachment(db, webRoot, AttachmentE1, RequestE1Read, now);
        AddAttachment(db, webRoot, AttachmentE2, RequestE2Read, now);
        AddAttachment(db, webRoot, AttachmentE1DeleteSelf, RequestE1Read, now);
        AddAttachment(db, webRoot, AttachmentE1DeleteOfficer, RequestE1Read, now);
    }

    /// <summary>Removes every attachment folder this fixture's requests own, including files uploaded by cases.</summary>
    internal static void DeleteFiles(string webRoot)
    {
        foreach (var requestId in RequestIds)
        {
            var folder = Path.Combine(webRoot, "attachments", "Leave", requestId.ToString(System.Globalization.CultureInfo.InvariantCulture));
            if (Directory.Exists(folder))
                Directory.Delete(folder, recursive: true);
        }
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

    private static void AddAttachment(AppDbContext db, string webRoot, int attachmentId, int requestId, DateTime now)
    {
        var relativePath = AttachmentRelativePath(requestId, attachmentId);
        var physicalPath = PhysicalPath(webRoot, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(physicalPath)!);
        File.WriteAllText(physicalPath, $"B06 test attachment {attachmentId}");

        db.LeaveRequestAttachments.Add(new LeaveRequestAttachment
        {
            Id = attachmentId,
            LeaveRequestId = requestId,
            FileName = $"b06-{attachmentId}.txt",
            ContentType = "text/plain",
            FileSizeBytes = new FileInfo(physicalPath).Length,
            StoragePath = relativePath,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        });
    }
}
