namespace SuCoS.Models;

/// <summary>
/// Page resources. All files that accompany a page.
/// </summary>
public class Resource : IResource
{
    /// <inheritdoc/>
    public required ISite Site { get; init; }

    /// <inheritdoc/>
    public string? Title { get; set; }

    /// <inheritdoc/>
    public required string SourceRelativePath { get; init; }

    #region IOutput

    /// <inheritdoc/>
    public string RelPermalink { get; set; } = string.Empty;

    #endregion IOutput

    #region IParams

    /// <inheritdoc/>
    public Dictionary<string, object> Params { get; set; } = [];

    #endregion IParams
}
