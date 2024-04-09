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
    public required string SourceFullPath { get; set; }

    /// <inheritdoc/>
    public string? SourceRelativePath => null;

    /// <inheritdoc/>
    public string? Permalink { get; set; }

    /// <inheritdoc/>
    public Dictionary<string, object> Params { get; set; } = [];
}
