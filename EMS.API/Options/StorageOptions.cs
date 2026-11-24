namespace EMS.API.Options;

/// <summary>
/// Where private uploads (employee documents and leave attachments) are kept. Organisation logos are public and
/// stay under the web root; see <c>.cursor/rules/shared-attachment-storage-convention.mdc</c>.
/// </summary>
public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    /// <summary>
    /// Private storage root. A relative path resolves against the content root; an absolute path is used as given.
    /// It must not be the web root or inside it (the API refuses to start).
    /// </summary>
    public string PrivateRoot { get; set; } = "App_Data/private";

    /// <summary>
    /// Runs the one-off move of document and leave attachment files from <c>wwwroot/attachments</c> into the
    /// private root at start-up. Safe to leave on: a second run moves nothing.
    /// </summary>
    public bool MovePublicFilesOnStartup { get; set; }
}
