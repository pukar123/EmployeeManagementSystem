namespace EMS.Domain.DbModels;

/// <summary>
/// Link table between employees and documents. Keeps association lifecycle separate from file metadata.
/// </summary>
public class EmployeeDocument
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }

    public int DocumentId { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsDeleted { get; set; } = false;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public Employee Employee { get; set; } = null!;

    public Document Document { get; set; } = null!;
}
