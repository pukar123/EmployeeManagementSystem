namespace EMS.Application.DTOs.Document;

public class DocumentTypeResponseModel
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}
