using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SuCoS.Models;

namespace SuCoS.Helpers;

/// <summary>
/// Helper methods for scanning files.
/// </summary>
public static class FileUtils
{
    /// <summary>
    /// Gets all Markdown files in the specified directory.
    /// </summary>
    /// <param name="directory">The directory path.</param>
    /// <param name="basePath">The initial directory path.</param>
    /// <returns>The list of Markdown file paths.</returns>
    public static IEnumerable<string> GetAllMarkdownFiles(string directory, string? basePath = null)
    {
        basePath ??= directory;
        var files = Directory.GetFiles(directory, "*.md").ToList();

        var subdirectories = Directory.GetDirectories(directory);
        foreach (var subdirectory in subdirectories)
        {
            files.AddRange(GetAllMarkdownFiles(subdirectory, basePath));
        }

        return files;
    }

    /// <summary>
    /// Gets the content of a template file based on the frontmatter and the theme path.
    /// </summary>
    /// <param name="themePath">The theme path.</param>
    /// <param name="frontmatter">The frontmatter to determine the template index.</param>
    /// <param name="site">Site data.</param>
    /// <param name="isBaseTemplate">Indicates whether the template is a base template.</param>
    /// <returns>The content of the template file.</returns>
    public static string GetTemplate(string themePath, Frontmatter frontmatter, Site site, bool isBaseTemplate = false)
    {
        if (frontmatter is null)
        {
            throw new ArgumentNullException(nameof(frontmatter));
        }
        if (site is null)
        {
            throw new ArgumentNullException(nameof(site));
        }

        var index = (frontmatter.Section, frontmatter.Kind, frontmatter.Type);

        var cache = isBaseTemplate ? site.baseTemplateCache : site.contentTemplateCache;

        // Check if the template content is already cached
        if (cache.TryGetValue(index, out var content))
            return content;

        var templatePaths = GetTemplateLookupOrder(themePath, frontmatter, isBaseTemplate);
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
        if (templatePaths is null)
        {
            throw new ArgumentNullException(nameof(templatePaths));
        }

        // Iterate through the template paths and return the content of the first existing file
        foreach (var templatePath in templatePaths.Where(File.Exists))
        {
            return File.ReadAllText(templatePath);
        }

        return string.Empty;
    }

    /// <summary>
    /// Gets the lookup order for template files based on the theme path, frontmatter, and template type.
    /// </summary>
    /// <param name="themePath">The theme path.</param>
    /// <param name="frontmatter">The frontmatter to determine the template index.</param>
    /// <param name="isBaseTemplate">Indicates whether the template is a base template.</param>
    /// <returns>The list of template paths in the lookup order.</returns>
    private static List<string> GetTemplateLookupOrder(string themePath, Frontmatter frontmatter, bool isBaseTemplate)
    {
        if (frontmatter is null)
        {
            throw new ArgumentNullException(nameof(frontmatter));
        }

        // Generate the lookup order for template files based on the theme path, frontmatter section, type, and kind
        var sections = frontmatter.Section is not null ? new[] { frontmatter.Section, string.Empty } : new[] { string.Empty };
        var types = new[] { frontmatter.Type, "_default" };
        var kinds = isBaseTemplate
            ? new[] { frontmatter.Kind + "-baseof", "baseof" }
            : new[] { frontmatter.Kind.ToString() };

        // for each section, each type and each kind
        return (from section in sections
                from type in types
                from kind in kinds
                let path = Path.Combine(themePath, section, type!, kind) + ".liquid"
                select path).ToList();
    }
}
