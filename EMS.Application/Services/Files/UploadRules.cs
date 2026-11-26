using EMS.Application.DTOs.Files;
using EMS.Domain.Enums;

namespace EMS.Application.Services.Files;

/// <summary>
/// The single source of truth for upload types and sizes (B10). Storage classes, the request size filter and the
/// checker read these rules; none keeps its own list or maximum. To add a type, add one entry and its tests.
/// </summary>
public static class UploadRules
{
    public const long OneMegabyte = 1024 * 1024;

    public const string DocxContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

    public static readonly AllowedUploadType Pdf = new(
        "PDF", [".pdf"], ["application/pdf"], UploadSignature.Pdf, ".pdf", "application/pdf", DocumentFileKind.Pdf);

    public static readonly AllowedUploadType Docx = new(
        "Word (.docx)", [".docx"], [DocxContentType], UploadSignature.Docx, ".docx", DocxContentType, DocumentFileKind.Word);

    public static readonly AllowedUploadType Png = new(
        "PNG", [".png"], ["image/png"], UploadSignature.Png, ".png", "image/png", DocumentFileKind.Image);

    public static readonly AllowedUploadType Jpeg = new(
        "JPEG", [".jpg", ".jpeg"], ["image/jpeg", "image/jpg"], UploadSignature.Jpeg, ".jpg", "image/jpeg", DocumentFileKind.Image);

    public static readonly AllowedUploadType WebP = new(
        "WebP", [".webp"], ["image/webp"], UploadSignature.WebP, ".webp", "image/webp", DocumentFileKind.Image);

    public static readonly UploadRule EmployeeDocument = new(
        UploadPurpose.EmployeeDocument, 15 * OneMegabyte, [Pdf, Docx, Png, Jpeg], "PDF, Word (.docx), PNG, JPEG", "document");

    public static readonly UploadRule LeaveAttachment = new(
        UploadPurpose.LeaveAttachment, 15 * OneMegabyte, [Pdf, Png, Jpeg], "PDF, PNG, JPEG", "attachment");

    public static readonly UploadRule OrganizationLogo = new(
        UploadPurpose.OrganizationLogo, 5 * OneMegabyte, [Png, Jpeg, WebP], "PNG, JPEG, WebP", "logo");

    public static UploadRule For(UploadPurpose purpose) => purpose switch
    {
        UploadPurpose.EmployeeDocument => EmployeeDocument,
        UploadPurpose.LeaveAttachment => LeaveAttachment,
        UploadPurpose.OrganizationLogo => OrganizationLogo,
        _ => throw new ArgumentOutOfRangeException(nameof(purpose), purpose, "Unknown upload purpose."),
    };
}
