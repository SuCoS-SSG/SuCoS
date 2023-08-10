using Microsoft.Extensions.FileSystemGlobbing;

namespace SuCoS.Models;

/// <summary>
/// Basic structure needed to generate user content and system content
/// </summary>
public interface IFrontMatterResources : IParams
{
    /// <summary>
    /// The resource filename search
    /// </summary>
    string Src { get; set; }

    /// <summary>
    /// The resource Title.
    /// </summary>
    string? Title { get; set; }

    /// <summary>
    /// The resource file name.
    /// </summary>
    string? Name { get; set; }

    /// <summary>
    /// Glob matcher that will parse the Src.
    /// </summary>
    public Matcher? GlobMatcher { get; set; }
}
