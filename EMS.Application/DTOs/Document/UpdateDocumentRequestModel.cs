namespace EMS.Application.DTOs.Document;

public class UpdateDocumentRequestModel
{
    public string Name { get; set; } = string.Empty;

    public int DocumentTypeId { get; set; }

    public int? EmployeeId { get; set; }

    public DateTime? IssueDate { get; set; }

    public DateTime? ExpiryDate { get; set; }
}
