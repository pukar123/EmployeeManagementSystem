namespace EMS.Domain.DbModels;

public class DocumentType
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public ICollection<Document> Documents { get; set; } = new List<Document>();
}
