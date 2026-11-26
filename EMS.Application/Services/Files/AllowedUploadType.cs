using EMS.Domain.Enums;

namespace EMS.Application.Services.Files;

/// <summary>
/// One allowed upload type: the extensions and declared content types accepted for it, the signature its content
/// must have, and what the server stores (extension, content type and document kind).
/// </summary>
public sealed class AllowedUploadType
{
    public AllowedUploadType(
        string name,
        IReadOnlyList<string> extensions,
        IReadOnlyList<string> contentTypes,
        UploadSignature signature,
        string serverExtension,
        string canonicalContentType,
        DocumentFileKind fileKind)
    {
        Name = name;
        Extensions = extensions;
        ContentTypes = contentTypes;
        Signature = signature;
        ServerExtension = serverExtension;
        CanonicalContentType = canonicalContentType;
        FileKind = fileKind;
    }

    /// <summary>Short name for messages and logs (for example <c>PDF</c>).</summary>
    public string Name { get; }

    /// <summary>Accepted extensions, lower case with the dot.</summary>
    public IReadOnlyList<string> Extensions { get; }

    /// <summary>Accepted declared content types, lower case, without parameters.</summary>
    public IReadOnlyList<string> ContentTypes { get; }

    public UploadSignature Signature { get; }

    /// <summary>Extension the server stores the file with.</summary>
    public string ServerExtension { get; }

    /// <summary>Content type the server stores and serves the file with.</summary>
    public string CanonicalContentType { get; }

    public DocumentFileKind FileKind { get; }
}
