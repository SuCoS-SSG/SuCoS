using Serilog;
using SuCoS.Helpers;
using SuCoS.Models;
using SuCoS.Models.CommandLineOptions;
using SuCoS.Parsers;

namespace SuCoS.Commands;

/// <summary>
/// Check links of a given site.
/// </summary>
public sealed class NewSiteCommand(NewSiteOptions options, ILogger logger, IFileSystem fileSystem, ISite site)
{
    private static SiteSettings _siteSettings = null!;

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

        _siteSettings = new SiteSettings
        {
            Title = options.Title,
            Description = options.Description,
            BaseUrl = options.BaseUrl
        };

        var site = new Site(
        new GenerateOptions()
        {
            SourceOption = options.Output
        },
        _siteSettings, new YamlParser(), null!, null);
        return new NewSiteCommand(options, logger, fileSystem, site);
    }

    /// <summary>
    /// Run the app
    /// </summary>
    /// <returns></returns>
    public int Run()
    {
        var outputPath = Path.GetFullPath(options.Output);
        var siteSettingsPath = Path.Combine(outputPath, "sucos.yaml");

        if (fileSystem.FileExists(siteSettingsPath) && !options.Force)
        {
            logger.Error("{directoryPath} already exists", outputPath);
            return 1;
        }

        logger.Information("Creating a new site: {title} at {outputPath}", site.Title, outputPath);

        try
        {
            CreateFolders(site.SourceFolders);
            site.Parser.SerializeAndSave(_siteSettings, siteSettingsPath);
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
            fileSystem.DirectoryCreateDirectory(folder);
        }
    }
}
