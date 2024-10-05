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
    public required string SourceFullPath { get; init; }

    /// <inheritdoc/>
    public string? SourceRelativePath => null;

    #region IOutput

    /// <inheritdoc/>
    public string? Permalink { get; set; }

    #endregion IOutput

    #region IParams

    /// <inheritdoc/>
    public Dictionary<string, object> Params { get; set; } = [];

    #endregion IParams
}
