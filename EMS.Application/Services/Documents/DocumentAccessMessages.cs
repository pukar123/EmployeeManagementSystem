namespace EMS.Application.Services.Documents;

/// <summary>
/// Messages for document access decisions (B09). The controller turns <see cref="Denied"/> into 403 with no body,
/// before its usual "business rule broken" 400.
/// </summary>
public static class DocumentAccessMessages
{
    public const string Denied = "Document access denied.";
}
