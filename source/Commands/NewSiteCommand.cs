using Serilog;
using SuCoS.Models;
using SuCoS.Models.CommandLineOptions;
using SuCoS.Parser;

namespace SuCoS;

/// <summary>
/// Check links of a given site.
/// </summary>
public sealed partial class NewSiteCommand(NewSiteOptions options, ILogger logger, IFileSystem fileSystem, ISite site)
{
    private static SiteSettings siteSettings = null!;

    /// <summary>
    /// Generate the needed data for the class
    /// </summary>
    /// <param name="options"></param>
    /// <param name="logger"></param>
    /// <param name="fileSystem"></param>
    /// <returns></returns>
    public static NewSiteCommand Create(NewSiteOptions options, ILogger logger, IFileSystem fileSystem)
    {
        ArgumentNullException.ThrowIfNull(options);

        siteSettings = new SiteSettings()
        {
            Title = options.Title,
            Description = options.Description,
            BaseURL = options.BaseURL
        };

        var site = new Site(new GenerateOptions() { SourceOption = options.Output }, siteSettings, new YAMLParser(), null!, null);
        return new NewSiteCommand(options, logger, fileSystem, site);
    }

    /// <summary>
    /// Run the app
    /// </summary>
    /// <returns></returns>
    public int Run()
    {
        var outputPath = fileSystem.GetFullPath(options.Output);
        var siteSettingsPath = fileSystem.Combine(outputPath, "sucos.yaml");

        if (fileSystem.FileExists(siteSettingsPath) && !options.Force)
        {
            logger.Error("{directoryPath} already exists", outputPath);
            return 1;
        }

        logger.Information("Creating a new site: {title} at {outputPath}", site.Title, outputPath);

        try
        {
            CreateFolders(site.SourceFolders);
            site.Parser.Export(siteSettings, siteSettingsPath);
        }
        catch (Exception ex)
        {
            logger.Error("Failed to export site settings: {ex}", ex);
            return 1;
        }

        logger.Information("Done");
        return 0;
    }

    /// <summary>
    /// Create the standard folders
    /// </summary>
    /// <param name="folders"></param>
    private void CreateFolders(IEnumerable<string> folders)
    {
        foreach (var folder in folders)
        {
            logger.Information("Creating {folder}", folder);
            fileSystem.CreateDirectory(folder);
        }
    }
}