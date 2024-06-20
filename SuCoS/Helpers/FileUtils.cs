using SuCoS.Models;

namespace SuCoS.Helpers;

/// <summary>
/// Helper methods for scanning files.
/// </summary>
public static class FileUtils
{
    /// <summary>
    /// Gets the content of a template file based on the page and the theme path.
    /// </summary>
    /// <param name="themePath">The theme path.</param>
    /// <param name="page">The page to determine the template index.</param>
    /// <param name="cacheManager">Site data.</param>
    /// <param name="isBaseTemplate">Indicates whether the template is a base template.</param>
    /// <returns>The content of the template file.</returns>
    public static string GetTemplate(string themePath, Page page, SiteCacheManager cacheManager, bool isBaseTemplate = false)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentNullException.ThrowIfNull(cacheManager);

        var index = (page.Section, page.Kind, page.Type);

        var cache = isBaseTemplate ? cacheManager.BaseTemplateCache : cacheManager.ContentTemplateCache;

        // Check if the template content is already cached
        if (cache.TryGetValue(index, out var content))
        {
            return content;
        }

        var templatePaths = GetTemplateLookupOrder(themePath, page, isBaseTemplate);
        content = GetTemplate(templatePaths);

        // Cache the template content for future use
        lock (cache)
        {
            _ = cache.TryAdd(index, content);
        }

        return content;
    }

    /// <summary>
    /// Gets the content of a template file from the specified list of template paths.
    /// </summary>
    /// <param name="templatePaths">The list of template paths to search.</param>
    /// <returns>The content of the template file, or an empty string if not found.</returns>
    private static string GetTemplate(List<string> templatePaths)
    {
        ArgumentNullException.ThrowIfNull(templatePaths);

        // Iterate through the template paths and return the content of the first existing file
        foreach (var templatePath in templatePaths.Where(File.Exists))
        {
            return File.ReadAllText(templatePath);
        }

        return string.Empty;
    }

    /// <summary>
    /// Gets the lookup order for template files based on the theme path, page, and template type.
    /// </summary>
    /// <param name="themePath">The theme path.</param>
    /// <param name="page">The page to determine the template index.</param>
    /// <param name="isBaseTemplate">Indicates whether the template is a base template.</param>
    /// <returns>The list of template paths in the lookup order.</returns>
    private static List<string> GetTemplateLookupOrder(string themePath, Page page, bool isBaseTemplate)
    {
        ArgumentNullException.ThrowIfNull(page);

        // Generate the lookup order for template files based on the theme path, page section, type, and kind
        string[] sections = page.Section is not null ? [page.Section, string.Empty] : [string.Empty];
        var types = new[] { page.Type, "_default" };

        // Get all the kinds including the "sub-values"
        var kinds = GetAllKinds(page.Kind, isBaseTemplate);

        if (isBaseTemplate)
        {
            kinds = kinds.Append("baseof");
        }

        // for each section, each type and each kind
        return sections
            .SelectMany(section => types.Select(type => new { section, type }))
            .SelectMany(x => kinds.Select(kind => new { x.section, x.type, kind }))
            .Select(x => Path.Combine(themePath, x.section, x.type!, x.kind) + ".liquid")
            .ToList();
    }

    private static IEnumerable<string> GetAllKinds(Kind kind, bool isBaseTemplate) =>
        Enum.GetValues(typeof(Kind))
            .Cast<Kind>()
            .Where(k => kind.HasFlag(k))
            .OrderByDescending(k => k)
            .Select(k => isBaseTemplate
                ? k.ToString().ToLower(System.Globalization.CultureInfo.CurrentCulture) + "-baseof"
                : k.ToString().ToLower(System.Globalization.CultureInfo.CurrentCulture));
}
