using System;
using System.IO;
using Fluid;
using Markdig;
using Microsoft.Extensions.FileProviders;
using Serilog;
using SuCoS.Models;
using SuCoS.Models.CommandLineOptions;
using SuCoS.Parser;

namespace SuCoS.Helpers;

/// <summary>
/// Helper methods for scanning files.
/// </summary>
public static class SiteHelper
{
    /// <summary>
    /// Markdig 20+ built-in extensions
    /// </summary>
    /// https://github.com/xoofx/markdig
    public static readonly MarkdownPipeline MarkdownPipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();

    /// <summary>
    /// Creates the pages dictionary.
    /// </summary>
    /// <exception cref="FormatException"></exception>
    public static Site Init(string configFile, IGenerateOptions options, IFrontMatterParser frontMatterParser, FilterDelegate whereParamsFilter, ILogger logger, StopwatchReporter stopwatch)
    {
        if (stopwatch is null)
        {
            throw new ArgumentNullException(nameof(stopwatch));
        }

        SiteSettings siteSettings;
        try
        {
            siteSettings = ParseSettings(configFile, options, frontMatterParser);
        }
        catch
        {
            throw new FormatException("Error reading app config");
        }

        var site = new Site(options, siteSettings, frontMatterParser, logger, null);

        // Liquid template options, needed to theme the content 
        // but also parse URLs
        site.TemplateOptions.Filters.AddFilter("whereParams", whereParamsFilter);

        site.ResetCache();

        stopwatch.Start("Parse");

        site.ParseAndScanSourceFiles(site.SourceContentPath);

        stopwatch.Stop("Parse", site.filesParsedToReport);

        site.TemplateOptions.FileProvider = new PhysicalFileProvider(Path.GetFullPath(site.SourceThemePath));

        return site;
    }

    /// <summary>
    /// Get the section name from a file path
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns></returns>
    public static string GetSection(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            return string.Empty;
        }

        // Split the path into individual folders
        var folders = filePath.Split(Path.DirectorySeparatorChar);

        // Retrieve the first folder
        foreach (var folder in folders)
        {
            if (!string.IsNullOrEmpty(folder))
            {
                return folder;
            }
        }

        return string.Empty;
    }

    /// <summary>
    /// Reads the application settings.
    /// </summary>
    /// <param name="options">The generate options.</param>
    /// <param name="frontMatterParser">The front matter parser.</param>
    /// <param name="configFile">The site settings file.</param>
    /// <returns>The site settings.</returns>
    private static SiteSettings ParseSettings(string configFile, IGenerateOptions options, IFrontMatterParser frontMatterParser)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }
        if (frontMatterParser is null)
        {
            throw new ArgumentNullException(nameof(frontMatterParser));
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
            var siteSettings = frontMatterParser.ParseSiteSettings(fileContent) ?? throw new FormatException("Error reading app config");
            return siteSettings;
        }
        catch
        {
            throw new FormatException("Error reading app config");
        }
    }
}
