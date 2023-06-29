using System;
using System.Collections.Generic;
using System.IO;
using SuCoS.Models;

namespace SuCoS.Helper;

/// <summary>
/// Helper methods for scanning files.
/// </summary>
public static class FileUtils
{
    /// <summary>
    /// Gets all Markdown files in the specified directory.
    /// </summary>
    /// <param name="directory">The directory path.</param>
    /// <returns>The list of Markdown file paths.</returns>
    public static List<string> GetAllMarkdownFiles(string directory)
    {
        var markdownFiles = new List<string>();
        var files = Directory.GetFiles(directory, "*.md");
        markdownFiles.AddRange(files);

        var subdirectories = Directory.GetDirectories(directory);
        foreach (var subdirectory in subdirectories)
        {
            markdownFiles.AddRange(GetAllMarkdownFiles(subdirectory));
        }

        return markdownFiles;
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
        if (!cache.TryGetValue(index, out var content))
        {
            var templatePaths = GetTemplateLookupOrder(themePath, frontmatter, isBaseTemplate);
            content = GetTemplate(templatePaths);

            // Cache the template content for future use
            lock (cache)
            {
                _ = cache.TryAdd(index, content);
            }
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
        foreach (var templatePath in templatePaths)
        {
            if (File.Exists(templatePath))
            {
                return File.ReadAllText(templatePath);
            }
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
    public static List<string> GetTemplateLookupOrder(string themePath, Frontmatter frontmatter, bool isBaseTemplate)
    {
        if (frontmatter is null)
        {
            throw new ArgumentNullException(nameof(frontmatter));
        }

        // Generate the lookup order for template files based on the theme path, frontmatter section, type, and kind
        var sections = frontmatter.Section is not null ? new string[] { frontmatter.Section.ToString(), string.Empty } : new string[] { string.Empty };
        var types = new string[] { frontmatter.Type.ToString(), "_default" };
        var kinds = isBaseTemplate
            ? new string[] { frontmatter.Kind.ToString() + "-baseof", "baseof" }
            : new string[] { frontmatter.Kind.ToString() };
        var templatePaths = new List<string>();
        foreach (var section in sections)
        {
            foreach (var type in types)
            {
                foreach (var kind in kinds)
                {
                    var path = Path.Combine(themePath, section, type, kind) + ".liquid";
                    templatePaths.Add(path);
                }
            }
        }
        return templatePaths;
    }
}
