namespace EMS.Application.DTOs.Files;

/// <summary>What an uploaded file is for. Each purpose has its own allowed types and maximum size (B10).</summary>
public enum UploadPurpose
{
    EmployeeDocument = 1,
    LeaveAttachment = 2,
    OrganizationLogo = 3,
}
