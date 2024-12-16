namespace SuCoS.Models;

/// <summary>
/// Raw resource representation for ContentSource
/// </summary>
public class ContentSourceResource
{
    /// <summary>
    /// Relative path of the resource
    /// </summary>
    public required string SourceRelativePath { get; set; }

    /// <summary>
    /// Additional parameters for the resource
    /// </summary>
    public Dictionary<string, object> Params { get; set; } = [];

    /// <summary>
    /// The final resource object from this
    /// </summary>
    public Resource? Resource { get; internal set; }
}
