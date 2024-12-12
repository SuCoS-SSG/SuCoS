namespace SuCoS.Models;

/// <summary>
/// Page resources. All files that accompany a page.
/// </summary>
public class Resource : IResource
{
    /// <inheritdoc/>
    public string? Title { get; set; }

    /// <inheritdoc/>
    public string? FileName { get; set; }

    /// <inheritdoc/>
    public required string SourceRelativePath { get; init; }

    #region IOutput

    /// <inheritdoc/>
    public string? Permalink { get; set; }

    /// <inheritdoc/>
    public string? RelPermalinkDir { get; set; }

    /// <inheritdoc/>
    public string? RelPermalinkFilename { get; set; }

    #endregion IOutput

    #region IParams

    /// <inheritdoc/>
    public Dictionary<string, object> Params { get; set; } = [];

    #endregion IParams
}
