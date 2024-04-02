using SuCoS.Models;
using System.Collections.Concurrent;

namespace SuCoS.Helpers;

/// <summary>
/// Manages all the lists and dictionaries used for cache for the site
/// </summary>
public class SiteCacheManager
{
    /// <summary>
    /// Cache for content templates.
    /// </summary>
    public Dictionary<(string?, Kind?, string?), string> contentTemplateCache { get; } = [];

    /// <summary>
    /// Cache for base templates.
    /// </summary>
    public Dictionary<(string?, Kind?, string?), string> baseTemplateCache { get; } = [];

    /// <summary>
    /// Cache for tag page.
    /// </summary>
    public ConcurrentDictionary<string, Lazy<IPage>> automaticContentCache { get; } = new();

    /// <summary>
    /// Resets the template cache to force a reload of all templates.
    /// </summary>
    public void ResetCache()
    {
        baseTemplateCache.Clear();
        contentTemplateCache.Clear();
        automaticContentCache.Clear();
    }
}
