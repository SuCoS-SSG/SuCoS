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
    public Dictionary<(string?, Kind?, string?), string> ContentTemplateCache { get; } = [];

    /// <summary>
    /// Cache for base templates.
    /// </summary>
    public Dictionary<(string?, Kind?, string?), string> BaseTemplateCache { get; } = [];

    /// <summary>
    /// Cache for tag page.
    /// </summary>
    public ConcurrentDictionary<string, Lazy<IPage>> AutomaticContentCache { get; } = new();

    /// <summary>
    /// Resets the template cache to force a reload of all templates.
    /// </summary>
    public void ResetCache()
    {
        BaseTemplateCache.Clear();
        ContentTemplateCache.Clear();
        AutomaticContentCache.Clear();
    }
}
