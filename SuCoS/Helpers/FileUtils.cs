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
    /// <param name="page">The page to determine the template index.</param>
    /// <param name="themePath">The theme path.</param>
    /// <param name="cacheManager">Site data.</param>
    /// <param name="isBaseTemplate">Indicates whether the template is a base template.</param>
    /// <returns>The content of the template file.</returns>
    public static string GetTemplate(this Page page, string themePath,
        SiteCacheManager cacheManager, bool isBaseTemplate = false)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentNullException.ThrowIfNull(cacheManager);

        CacheTemplateIndex index = (page.Section, page.Kind, page.Type, page.OutputFormat);

        var cache = isBaseTemplate
            ? cacheManager.BaseTemplateCache
            : cacheManager.ContentTemplateCache;

        // Check if the template content is already cached
        if (cache.TryGetValue(index, out var content))
        {
            return content;
        }

        var templatePaths = page.GetTemplateLookupOrder(isBaseTemplate);
        content = ReadTemplate(templatePaths, themePath);

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
    /// <param name="themePath">The theme path.</param>
    /// <returns>The content of the template file, or an empty string if not found.</returns>
    private static string ReadTemplate(IEnumerable<string> templatePaths,
        string themePath)
    {
        ArgumentNullException.ThrowIfNull(templatePaths);

        // Iterate through the template paths and return the content of the first existing file
        foreach (var templatePath in templatePaths
                                     .Select(templatePath =>
                                         Path.Combine(themePath, templatePath))
                                     .Where(File.Exists))
        {
            return File.ReadAllText(templatePath);
        }

        return string.Empty;
    }

    /// <summary>
    /// Gets the lookup order for template files based on the theme path, page, and template type.
    /// </summary>
    /// <param name="page">The page to determine the template index.</param>
    /// <param name="isBaseTemplate">Indicates whether the template is a base template.</param>
    /// <returns>The list of template paths in the lookup order.</returns>
    public static IEnumerable<string> GetTemplateLookupOrder(this Page page, bool isBaseTemplate)
    {
        ArgumentNullException.ThrowIfNull(page);

        // Generate the lookup order for template files based on the theme path, page section, type, and kind
        string[] sections = page.Section is null
            ? [string.Empty]
            : [page.Section, string.Empty];
        string[] types = page.Type is null
            ? [string.Empty, "_default"]
            : [page.Type, string.Empty, "_default"];
        string[] outputFormats = ["." + page.OutputFormatObj!.Extension, string.Empty];

        // Get all the kinds including the "sub-values"
        var kinds = isBaseTemplate ? GetAllKindsBase(page.Kind) : GetAllKinds(page.Kind);

        // for each section, each type and each kind
        return sections
            .SelectMany(section =>
                types.Select(type => new { section, type }))
            .SelectMany(x =>
                kinds.Select(kind => new { x.section, x.type, kind }))
            .SelectMany(x => outputFormats.Select(outputFormat =>
            new { x.section, x.type, x.kind, outputFormat }))
            .Select(x =>
                Path.Combine(x.section, x.type, x.kind) + x.outputFormat + ".liquid")
            .Distinct();
    }

    private static IEnumerable<string> GetAllKinds(Kind kind) =>
        Enum.GetValues(typeof(Kind))
            .Cast<Kind>()
            .Where(k => kind.HasFlag(k))
            .OrderByDescending(kind => kind)
            .Select(kind => kind.ToString());

    private static IEnumerable<string> GetAllKindsBase(Kind kind) =>
        GetAllKinds(kind)
            .Select(kind => kind + "-baseof")
            .Append("baseof");

    /// <summary>
    /// Default Output Formats
    /// </summary>
    public static readonly Dictionary<string, OutputFormat> OutputFormats =
        new()
        {
            {
                "html", new OutputFormat
                {
                    Extension = "html"
                }
            },
            {
                "rss", new OutputFormat
                {
                    Extension = "xml",
                    NoUgly = true
                }
            },
            {
                "robots",
                new OutputFormat
                {
                    BaseName = "robots",
                    Extension = "xml",
                    NoUgly = true
                }
            }
        };
}
