using Markdig;
using Serilog;
using SuCoS.Models;
using SuCoS.Models.CommandLineOptions;
using SuCoS.Parsers;

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
    /// Creates the pages' dictionary.
    /// </summary>
    /// <exception cref="FormatException"></exception>
    public static Site Init(string configFile, IGenerateOptions options, IFrontMatterParser parser, ILogger logger, StopwatchReporter stopwatch, IFileSystem fs)
    {
        ArgumentNullException.ThrowIfNull(stopwatch);
        ArgumentNullException.ThrowIfNull(fs);

        var siteSettings = ParseSettings(configFile, options, parser, fs);

        var site = new Site(options, siteSettings, parser, logger, null);

        site.ResetCache();

        stopwatch.Start("Parse");

        site.ScanAndParseSourceFiles(fs, site.SourceContentPath);

        stopwatch.Stop("Parse", site.FilesParsedToReport);

        stopwatch.Start("Generate Pages");

        site.ProcessPages();

        stopwatch.Stop("Generate Pages", site.FilesParsedToReport);

        if (fs.DirectoryExists(Path.GetFullPath(site.SourceThemePath)))
        {
            site.TemplateEngine.Initialize(site);
        }

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
    /// <param name="parser">The front matter parser.</param>
    /// <param name="fs"></param>
    /// <param name="configFile">The site settings file.</param>
    /// <returns>The site settings.</returns>
    public static SiteSettings ParseSettings(string configFile, IGenerateOptions options, IFrontMatterParser parser, IFileSystem fs)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(parser);
        ArgumentNullException.ThrowIfNull(fs);

        // Read the main configuration
        var filePath = Path.Combine(options.Source, configFile);
        if (!fs.FileExists(filePath))
        {
            throw new FileNotFoundException($"The {configFile} file was not found in the specified source directory: {options.Source}");
        }

        var fileContent = fs.FileReadAllText(filePath);
        var siteSettings = parser.Parse<SiteSettings>(fileContent)
            ?? throw new FormatException($"Error reading app config {configFile}");
        return siteSettings;
    }
}
