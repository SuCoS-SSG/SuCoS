using System;
using System.IO;
using Fluid;
using Microsoft.Extensions.FileProviders;
using Serilog;
using SuCoS.Models;
using SuCoS.Parser;

namespace SuCoS.Helper;

/// <summary>
/// Helper methods for scanning files.
/// </summary>
public static class SiteHelper
{

    /// <summary>
    /// Reads the application settings.
    /// </summary>
    /// <param name="options">The generate options.</param>
    /// <param name="frontmatterParser">The frontmatter parser.</param>
    /// <param name="configFile">The site settings file.</param>
    /// <param name="whereParamsFilter">The method to be used in the whereParams.</param>
    /// <param name="logger">The logger instance. Injectable for testing</param>
    /// <returns>The site settings.</returns>
    public static Site ParseSettings(string configFile, IGenerateOptions options, IFrontmatterParser frontmatterParser, FilterDelegate whereParamsFilter, ILogger logger)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }
        if (frontmatterParser is null)
        {
            throw new ArgumentNullException(nameof(frontmatterParser));
        }

        try
        {
            // Read the main configation
            var filePath = Path.Combine(options.Source, configFile);
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"The {configFile} file was not found in the specified source directory: {options.Source}");
            }

            var fileContent = File.ReadAllText(filePath);
            var site = frontmatterParser.ParseSiteSettings(fileContent);

            site.Logger = logger;
            site.options = options;
            site.SourceDirectoryPath = options.Source;
            site.OutputPath = options.Output;

            // Liquid template options, needed to theme the content 
            // but also parse URLs
            site.TemplateOptions.Filters.AddFilter("whereParams", whereParamsFilter);

            if (site is null)
            {
                throw new FormatException("Error reading app config");
            }
            return site;
        }
        catch
        {
            throw new FormatException("Error reading app config");
        }
    }


    /// <summary>
    /// Creates the pages dictionary.
    /// </summary>
    /// <exception cref="FormatException"></exception>
    public static Site Init(string configFile, IGenerateOptions options, IFrontmatterParser frontmatterParser, FilterDelegate whereParamsFilter, ILogger logger, StopwatchReporter stopwatch)
    {
        if (stopwatch is null)
        {
            throw new ArgumentNullException(nameof(stopwatch));
        }

        Site site;
        try
        {
            site = SiteHelper.ParseSettings(configFile, options, frontmatterParser, whereParamsFilter, logger);
        }
        catch
        {
            throw new FormatException("Error reading app config");
        }

        site.ResetCache();

        site.ScanAllMarkdownFiles();

        site.ParseSourceFiles(stopwatch);

        site.TemplateOptions.FileProvider = new PhysicalFileProvider(Path.GetFullPath(site!.SourceThemePath));

        return site;
    }

    /// <summary>
    /// Get the section name from a file path
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns></returns>
    public static string GetSection(string filePath)
    {
        // Split the path into individual folders
        var folders = filePath?.Split(Path.DirectorySeparatorChar);

        // Retrieve the first folder
        for (var i = 0; i < folders?.Length; i++)
        {
            if (!string.IsNullOrEmpty(folders[i]))
            {
                return folders[i];
            }
        }

        return string.Empty;
    }
}
