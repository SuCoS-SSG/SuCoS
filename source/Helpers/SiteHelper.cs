using System;
using System.IO;
using Fluid;
using Microsoft.Extensions.FileProviders;
using SuCoS.Models;
using SuCoS.Parser;

namespace SuCoS;

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
    /// <returns>The site settings.</returns>
    public static Site ParseSettings(string configFile, IGenerateOptions options, IFrontmatterParser frontmatterParser, FilterDelegate whereParamsFilter)
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

            site.options = options;
            site.SourceDirectoryPath = options.Source;
            site.OutputPath = options.Output;

            // Liquid template options, needed to theme the content 
            // but also parse URLs
            site.TemplateOptions.MemberAccessStrategy.Register<Frontmatter>();
            site.TemplateOptions.MemberAccessStrategy.Register<Site>();
            site.TemplateOptions.MemberAccessStrategy.Register<BaseGeneratorCommand>();
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
    public static Site Init(string configFile, IGenerateOptions options, IFrontmatterParser frontmatterParser, FilterDelegate whereParamsFilter, StopwatchReporter stopwatch)
    {
        if (stopwatch is null)
        {
            throw new ArgumentNullException(nameof(stopwatch));
        }

        Site site;
        try
        {
            site = SiteHelper.ParseSettings(configFile, options, frontmatterParser, whereParamsFilter);
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
}
