using Microsoft.Extensions.FileSystemGlobbing;
using YamlDotNet.Serialization;

namespace SuCoS.Models;

/// <summary>
/// Basic structure needed to generate user content and system content
/// </summary>
[YamlSerializable]
public class FrontMatterResources : IFrontMatterResources
{
    /// <inheritdoc/>
    public string Src { get; set; } = string.Empty;

    /// <inheritdoc/>
    public string? Title { get; set; }

    /// <inheritdoc/>
    public string? Name { get; set; }

    /// <inheritdoc/>
    public Dictionary<string, object> Params { get; set; } = [];

    /// <inheritdoc/>
    public Matcher? GlobMatcher { get; set; }

    /// <summary>
    /// Constructor
    /// </summary>
    public FrontMatterResources() { }
}
