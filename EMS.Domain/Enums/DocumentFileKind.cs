namespace EMS.Domain.Enums;

/// <summary>Category of stored file for validation and display (Word, PDF, or image).</summary>
public enum DocumentFileKind
{
    None = 0,
    Pdf = 1,
    Word = 2,
    Image = 3,
}
