using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using SuCoS.Models;

namespace SuCoS.Helpers;

/// <summary>
/// Manages all the lists and dictionaries used for cache for the site
/// </summary>
public class SiteCacheManager
{
    /// <summary>
    /// Cache for content templates.
    /// </summary>
    public readonly Dictionary<(string?, Kind?, string?), string> contentTemplateCache = new();

    /// <summary>
    /// Cache for base templates.
    /// </summary>
    public readonly Dictionary<(string?, Kind?, string?), string> baseTemplateCache = new();

    /// <summary>
    /// Cache for tag page.
    /// </summary>
    public readonly ConcurrentDictionary<string, Lazy<IPage>> automaticContentCache = new();

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