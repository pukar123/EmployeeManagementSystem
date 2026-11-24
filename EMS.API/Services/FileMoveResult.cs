namespace EMS.API.Services;

/// <summary>Counts from one run of <see cref="PublicToPrivateFileMover"/>.</summary>
public sealed class FileMoveResult
{
    /// <summary>Files of legacy rows moved from <c>wwwroot/attachments</c> into the private root.</summary>
    public int Moved { get; internal set; }

    /// <summary>Rows that already held a storage key.</summary>
    public int AlreadyPrivate { get; internal set; }

    /// <summary>Rows whose stored path was changed to the storage key.</summary>
    public int PathsUpdated { get; internal set; }

    /// <summary>Rows whose file was found in neither place (left unchanged so a later run can retry).</summary>
    public int Missing { get; internal set; }

    /// <summary>Rows or files with an unsafe or unknown path, or an IO error (left unchanged).</summary>
    public int Skipped { get; internal set; }

    /// <summary>Unreferenced files moved from <c>wwwroot/attachments/{Employee,Leave}</c>.</summary>
    public int OrphansMoved { get; internal set; }

    /// <summary>Files where a different file already exists at the private target (both kept).</summary>
    public int Conflicts { get; internal set; }
}
