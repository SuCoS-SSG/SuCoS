using System.Collections.Generic;

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
    public string SourceFullPath { get; set; }

    /// <inheritdoc/>
    public string? SourceRelativePath => throw new System.NotImplementedException();
    
    /// <inheritdoc/>
    public string? Permalink { get; set; }

    /// <inheritdoc/>
    public Dictionary<string, object> Params { get; set; } = new();

    /// <summary>
    /// Default constructor.
    /// </summary>
    /// <param name="path"></param>
    public Resource(string path)
    {
        SourceFullPath = path;
    }
}
